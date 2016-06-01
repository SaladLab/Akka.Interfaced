** WRITING IN PROCESS **

## Filter

### Introduction

- Filter can monitor and manipulate request and response.
- For example, when you need to log all requests and responses, filter can help you.

### Factory

- Scope
  - IFilterPerClassFactory: Create a filter per a class
  - IFilterPerClassMethodFactory: Create a filter per a method of class
  - IFilterPerInstanceFactory: Create a filter per an instance
  - IFilterPerInstanceMethodFactory: Create a filter per a method of an instance
  - IFilterPerInvokeFactory: Create a filter for every invocation.

### Filter

Events
  - Pre or Post
  - Request or Notification or Message

Chain:
  - Filter order
  - F(1).Pre -> F(2).Pre -> ... -> Handler -> ... -> F(2).Post -> F(1).Post
  - When one of pre-filters makes result, handler won't handle a request
    - But other filters will handle request so it is required to check `Handled` property of request context.

#### Request Filter

- IPreRequestFilter, IPreRequestAsyncFilter
- IPostRequestFilter, IPostRequestAsyncFilter

#### Notification Filter

- IPreNotificationFilter, IPreNotificationAsyncFilter
- IPostNotificationFilter, IPostNotificationAsyncFilter

#### Message Filter

- IPreMessageFilter, IPreMessageAsyncFilter
- IPostMessageFilter, IPostMessageAsyncFilter
