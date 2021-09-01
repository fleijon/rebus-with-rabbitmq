# Info
Example for how RabbitMQ can be used to distribute work on multiple workers.

We simulate long running tasks with a delay.

Publisher will publish tasks every 4 second with length of 5 and 10 seconds.

We can scale up number of subscribers in docker-compose.yml, with:
> deploy:
>
>   mode: replicated
>
>     replicas: 2

such that we can handle the work load.
2 subscribers is not enough to handle the frequency of tasks, as you can see in this example. Rebus will the throw exception that the message was not dispatched.

# How to run
> docker-compose up

