# ContainerHive

ContainerHive is a web API built using .NET 7 that provides docker container management and deployment functionality. This API allows users to manage Projects, add Deployment-Configurations and view all relevant Docker Images and Containers with State and Logs.

It also allows users to automate deployment via a Webhook to deploy or kill all configured Deployments.

**Note:** This Project is still in **WIP**. So expect some Bugs, incomplete Features...

## Getting Started

To use ContainerHive, you'll need to have .NET 7 installed on your machine. You can download it from the [official .NET website](https://dotnet.microsoft.com/download/dotnet/7.0).

Once you have .NET 7 installed, you can clone the repository from GitHub by running the following command:

```bash
git clone https://github.com/AlexMi-Ha/ContainerHive.git
```

After cloning the repository, you have to add a file named `appsettings.json` in `ContainerHive/ContainerHive/` which should look something like this:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiPrivateKey": "YOUR API PRIVATE KEY",
  "ConnectionStrings": {
    "DefaultConnection": "YOUR DATABASE CONNECTION STRING"
  },
  "DatabaseConfig": {
    "Version": "YOUR DATABASE VERSION"
  },
  "DockerDaemonSocket": "URL OF THE DOCKER SOCKET YOUR USING",
  "RepoPath": "PATH TO THE REPO WHERE ALL THE PROJECTS WILL BE STORED"
}
```



Finally, you can build and run the project using the following command:

```bash
dotnet run
```

This will start the API, which you can access at `http://localhost:5214/swagger/index.html` to open the swagger documentation.



## API Endpoints

The ContainerHive API has the following endpoints:

<details>

<summary>Projects</summary>

`POST /projects`

Creates a new Project



`PUT /projects`

Updates an existing Project



`GET /projects`

Retrieves a list of all Projects



`DELETE /projects/{id}`

Deletes a project with a given id



`GET /projects/{id}`

Retrieves a project with a given id



`GET /projects/{id}/deployments`

Retrieves all Deployment Configurations of a project



`POST /projects/{id}/webhook/regenerate`

Regenerates the Api Token for a project



`POST /projects/{id}/deploy`

Starts the Deployment Process (Does not wait for it to finish)



`POST /projects/{id}/kill`

Starts the Killing and Pruning Process (Does not wait for it to finish)

</details>

<details>

<summary>Deployments</summary>

`POST /deployments`

Creates a new Deployment Configuration



`PUT /deployments`

Updates a given Deployment Configuration



`GET /deployments`

Retrieves all Deployment Configurations (of every project - you may want to take a look at **/projects/{id}/deployments**)



`DELETE /deployments/{id}`

Deletes an existing Deployment Config with the specified id



`GET /deployments/{id}`

Retrieves the Deployment Config with the specified id

</details>



<details>

<summary>Webhooks</summary>

`POST /webhooks/{id}/deploy`

Starts the Deployment Process (Does not wait for it to finish)



`POST /webhooks/{id}/kill`

Starts the Killing and Pruning Process (Does not wait for it to finish)

</details>



## Authentication

All normal API Endpoints (*Projects*, *Deployments*) need a `x-api-private-token` Header to be set. Its value should be the ApiPrivateToken specified in your `appsettings.json`.

The Webhook endpoints need a `apiToken` in the Request-Body. This is the project specific Webhook-Token specified in the Project-Model. Note: The Project's `WebhookActive` property needs to be set to `true` for the request to go through!
