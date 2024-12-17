package main

import (
	"encoding/json"
	"log"
	"os"
	"sync"

	"github.com/sendgrid/sendgrid-go"
	"github.com/sendgrid/sendgrid-go/helpers/mail"
	"github.com/streadway/amqp"
)

type Request struct {
	To          string `json:"to"`
	Subject     string `json:"subject"`
	Body        string `json:"body"`
	Attachments []any  `json:"attachments"`
	Priority    string `json:"priority"`
}

type Message struct {
	Request Request `json:"request"`
	ID      string  `json:"id"`
}

type RabbitConsumer struct {
	connection *amqp.Connection
	queue      string
}

func NewRabbitConsumer(conn *amqp.Connection, queue string) *RabbitConsumer {
	return &RabbitConsumer{
		connection: conn,
		queue:      queue,
	}
}

func (c *RabbitConsumer) Consume() error {
	channel, err := c.connection.Channel()
	if err != nil {
		return err
	}
	defer channel.Close()

	msgs, err := channel.Consume(
		c.queue,
		"",
		true,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		return err
	}

	var wg sync.WaitGroup

	for d := range msgs {
		wg.Add(1)
		go func(d amqp.Delivery) {
			defer wg.Done()
			var message Message
			if err := json.Unmarshal(d.Body, &message); err != nil {
				log.Printf("Error decoding message: %v", err)
				return
			}
			c.handleMessage(message.Request, message.ID)
		}(d)
	}

	wg.Wait()

	log.Printf("Consumer started for queue: %s", c.queue)
	return nil
}

func (c *RabbitConsumer) handleMessage(msg Request, id string) {
	if msg.To == "" || msg.Subject == "" || msg.Body == "" {
		log.Printf("Invalid message: %+v", msg)
		return
	}

	from := mail.NewEmail(os.Getenv("SENDGRID_USER"), os.Getenv("SENDGRID_FROM"))
	to := mail.NewEmail("", msg.To)
	subject := msg.Subject + " - ID: " + id
	content := msg.Body + "\n\nMessage ID: " + id

	message := mail.NewSingleEmail(from, subject, to, content, content)

	client := sendgrid.NewSendClient(os.Getenv("SENDGRID_KEY"))

	// Send email asynchronously
	go func() {
		response, err := client.Send(message)
		if err != nil {
			log.Printf("Failed to send email. ID: %s, Error: %v", id, err)
			return
		}
		log.Printf("Email sent successfully. ID: %s, Status Code: %d", id, response.StatusCode)
	}()
}
