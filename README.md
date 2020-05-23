# webapi_core_using_azure_tablestorage
This is sample project which has a **CURD** operation using latest technologies.
This is just for the learning purpose I have this application.

### Technologies used
- Dot net core
- Azure table storage

### Concept
This project perform basic create, delete update and read operation against a azure table storage.

I used repository pattern to create this project. So that storage factory class will have best method to perform create, update, delete and read operations against Azure table storage.

For which table we are going to perform the create, update, delete read operation against Azure table storage will have a separate repository class.

if you wanted to save data against the different table will have to read this storage factory class and we can use all the methods the different table name to perform this operation.

if you want to use a different set of storage account to perform the similar operation against setup table then the storage factory class itself can be inherited and we can override the cup methods this can be achieved.
