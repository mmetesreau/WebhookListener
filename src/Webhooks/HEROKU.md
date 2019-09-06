# How to deploy to Heroku

## Requirements

You'll need [Heroku Command Line Interface](https://devcenter.heroku.com/articles/heroku-cli#download-and-install).

## Create an application

```
$ heroku login
$ heroku create [MY_APPLICATION]
$ heroku config:set GITHUB_SECRET=[MY_GITHUB_SECRET] -a [MY_APPLICATION]
```

## Deploy the application

```
$ heroku login
$ heroku container:login
$ heroku container:push web -a [MY_APPLICATION]
$ heroku container:release web -a [MY_APPLICATION]
```

If it's your first deployment, you need to scale one dyno for your application.

```
$ heroku ps:scale web=1 -a [MY_APPLICATION]
```

## See what's going on

```
$ heroku login
$ heroku logs --tail -a [MY_APPLICATION]
```

## CI/CD

Have a look at the [documentation](https://devcenter.heroku.com/articles/container-registry-and-runtime#using-a-ci-cd-platform).