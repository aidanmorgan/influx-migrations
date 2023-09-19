# Influx Migrations

An implementation of a migrations framework to perform database transformations against an InfluxDb database instance.

# Concepts

A ```Migration``` represents a sequence of ```Operations``` and ```Tasks``` that are performed to change the schema of a database from one ```version``` to another.


# Configuration

Migrations can be configured in two ways, either Code First or using YAML configuration files.


## YAML

YAML parsing is performed using a ```YamlMigrationParser```, with the specific operations and tasks implemented by ```IYamlOperationParser``` (with a ```[YamlOperationParser]``` attribute) and a ```IYamlTaskParser``` (with a ```[YamlTaskParser]``` on it).

Parsing of YAML files is performed using a ```YamlMigrationParser```. 

All operations that are able to be performed have a corresponding YAML parser combination.


## Code First

To perform code first migrations, define a class that implements ```InfluxMigrations.Core.ICodeFirstMigration``` and has an attribute of ```[InfluxMigration]``` on it.

Code First Migrations can be loaded using a ```CodeFirstMigrationLoaderService```.

# Supported Operations

| Operation                     | Description                                           |
|-------------------------------|--------------------------------------------------|
| Create Bucket                 | Creates a new bucket for a given name, organisation.               |
| Delete Bucket                 | Deletes a bucket from an organisation|
| Onboarding                    | Perform the initial onboarding process |
| Create User                   | Create a new user with provided password |
| Create Bucket Token           | Create an access token for a bucket with the provided permissions |
| Create Organisation           | Create a new organisation |
| Add User to Organisation      | Add a user (by name or id) to an Organisation (by name or id) |
| Remove User from Organisation | Remove a user (by name or id) from an Organisation (by name or id) |

# Resolvable Values

All operations and tasks support the ability to reference values from elsewhere in the migrations system, these include the following:

| Function                                            | Format                          | Example                          |
|-----------------------------------------------------|---------------------------------|----------------------------------|
| Value from an execute phase                         | ${result:<>}              | ${result.id}               |
| Value from a commit phase                           | ${commit:<>}              | ${commit.id}               |
| Value from a rollback phase                         | ${rollback:<>}            | ${rollback.id}               |
| Value from the environment where migrations are run | ${env:<>}                 | ${env:INFLUX_TOKEN}              |
| Value from the migration itself                     | ${migration:<>}           | ${migration:BUCKET_NAME}         |
| Value from the migration operation/task itself      | ${local:<>}               | ${local:BUCKET_NAME}             |
| Current time (UTC in UNIX Milliseconds)             | ${now}                          | ${now}                           |
| Value from a previous step                          | ${step:<>:<>} | ${step:step1:${result:bucketid}} |