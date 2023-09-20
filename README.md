# Influx Migrations

An implementation of a migrations framework to perform database transformations against an InfluxDb database instance.

# Concepts

A ```Migration``` represents a sequence of ```Operations``` and ```Tasks``` that are performed to change the schema of a database from one ```version``` to another.

## Phases

Each ```Operation``` in a ```Migration``` goes through a sequence of phases, they are:
* ```Execute``` - performs the operation
* ```Commit``` - performs any finalisation after all ```Operations``` in the ```Migration``` have completed
* ```Rollback``` - performs an undo of any changes as part of the ```Execute``` phase due to an ```Operation``` failing.


## Tasks

Each phase of an ```Operation``` and the ```Migration``` itself may also have a ```Task``` associated with it that is executed after all of the phase-operations have completed.

The following Tasks are provided:
* ```Echo``` - prints a value to the console
* ```Write File``` - writes a value to a file
* ```Insert Data``` - inserts data (in influx wire protocol) to a bucket
* ```Set Value``` - set a variable (in local, migration or global scope) that can be used in other operations/tasks.

## Resolving Runtime Values

All operations and tasks support the ability to reference values from elsewhere in the migration, allowing specific values (e.g. id's) to be used after they have been defined.

Resolvable values include the following:

| Function                                            | Format                          | Example                          |
|-----------------------------------------------------|---------------------------------|----------------------------------|
| Value from an execute phase                         | ${result:<>}              | ```${result:id}```               |
| Value from a commit phase                           | ${commit:<>}              | ```${commit:id}```               |
| Value from a rollback phase                         | ${rollback:<>}            | ```${rollback:id}```               |
| Value from the environment where migrations are run | ${env:<>}                 | ```${env:INFLUX_TOKEN}```              |
| Value from the migration itself                     | ${migration:<>}           | ```${migration:BUCKET_NAME}```         |
| Value from the migration operation/task itself      | ${local:<>}               | ```${local:BUCKET_NAME}```             |
| Current time (UTC in UNIX Milliseconds)             | ${now}                          | ```${now}```                           |
| Value from a previous step                          | ```${step:<>:<>}``` | ```${step:step1:${result:bucketid}}``` |


# Configuration

Migrations can be configured in two ways, either Code First or using YAML configuration files.

## YAML Configuration

YAML parsing is performed using a ```YamlMigrationParser```, with the specific operations and tasks implemented by ```IYamlOperationParser``` (with a ```[YamlOperationParser]``` attribute) and a ```IYamlTaskParser``` (with a ```[YamlTaskParser]``` on it).

Parsing of YAML files is performed using a ```YamlMigrationParser```. 

All operations that are able to be performed have a corresponding YAML parser combination.

An example migration YAML file is:

```
migration:
  version: 0002
  up:
    - operation: create-bucket
      id: one
      name: test-bucket1
      organisation: test-organisation
    - operation: create-bucket
      name: test-bucket2
      organisation_id: ${step:one:${result.organisationid}}
  down:
    - operation: delete-bucket
      name: test-bucket1
    - operation: delete-bucket
      name: test-bucket1
```

This migration is version 0002, and creates two buckets in the same organisation (for an up migration), or deletes the two buckets (for a down operation).

Note the use of a "resolvable" string in the example above for the organisation id of the second bucket, this value is derived at run-time based on the execution of the migration and allows values from previous steps to be used in later steps.



### Running YAML Migrations

A simple command-line application called ```InfluxMigrate``` is provided that will parse all .yml/.yaml files from a directory and execute the migrations against an influxdb instance (with history stored in the influx database).


## Code First Configuration
(currently very early alpha)

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

