Elmah-MongoDB
======================================================================
See: [Elmah Error Logging with MongoDB Official Driver](http://www.captaincodeman.com/2011/05/28/elmah-error-logging-official-10gen-mongodb-driver/)

## Overview
[ELMAH](http://code.google.com/p/elmah/) (Error Logging Modules and Handlers) is an application-wide error logging facility that is completely pluggable. It can be dynamically added to a running ASP.NET web application, or even all ASP.NET web applications on a machine, without any need for re-compilation or re-deployment.

This provider enables [MongoDB](http://www.mongodb.org/) to be used as the back-end storage via the [Official 10gen provider](http://www.mongodb.org/display/DOCS/CSharp+Language+Center).

## Usage
The easiest way to add this to a project is via the [elmah.mongodb NuGET package](http://nuget.org/List/Packages/elmah.mongodb) which will add the required assemblies to your project.

The provider supports multiple per-application collections within a single database and will automatically create a MongoDB Capped Collection called 'Elmah' if the application name is not set or will use Elmah-ApplicationName if it is.

The size of the MongoDB collection can be controlled by setting the maxSize (bytes) and maxDocuments 
properties. By default a 100mb collection is used (with no document limit).

## Configuration
Here is an example configuration:

    <elmah>
      <errorLog type="Elmah.MongoErrorLog, Elmah.MongoDB" connectionStringName="elmah-mongodb" maxSize="10485760" maxDocuments="10000"/>
    </elmah>
    <connectionStrings>
      <add name="elmah-mongodb" connectionString="mongodb://localhost/elmah?w=0"/>
    </connectionStrings>
