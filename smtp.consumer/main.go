package main

import (
	"log"
	"os"
	"os/signal"
	"runtime"
	"syscall"

	"github.com/joho/godotenv"
	"github.com/streadway/amqp"
)

func loadEnvironments() {
	if err := godotenv.Load(); err != nil {
		log.Fatalf("Error loading environments: %s", err)
	}
}

func main() {
	loadEnvironments()

	runtime.GOMAXPROCS(runtime.NumCPU())

	rabbitmqURL := os.Getenv("RABBITMQ_URL")
	if rabbitmqURL == "" {
		log.Fatal("RABBITMQ_URL environment variable is not set")
	}

	connection, err := amqp.Dial(rabbitmqURL)
	if err != nil {
		log.Fatalf("Failed to connect to RabbitMQ: %v", err)
	}
	defer connection.Close()

	rabbitConsumer := NewRabbitConsumer(connection, os.Getenv("RABBITMQ_QUEUE"))

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, os.Interrupt, syscall.SIGTERM)

	log.Println("Consumer is running. Press CTRL+C to exit.")

	go func() {
		<-quit
		log.Println("Shutting down gracefully...")
		connection.Close()
		os.Exit(0)
	}()

	if err := rabbitConsumer.Consume(); err != nil {
		log.Fatalf("Failed to start consuming: %v", err)
	}

	select {}
}
