Orchard-ClientConnector
=======================

Datwendo.ClientConnector Orchard Module

*Orchard users are welcome, Cloud Connector is in test period until the end of summer, send me an email to receive a dedicated free Connector with a longer free live delay than 14 days and other parameters than default demo's.*

It contains an attachable part which is in charge of generating an Identity using a Cloud Connector.
Very nice, you can attach it to any Content Type you create. 
There are 'Site Parameters' to fix global values as the Service Url for Datwendo, the validity delay for security keys, the Subscriber Id.
Other parameters are content-type dependant, they are grouped on each Content Type in such a way that there could be different Connectors for different Content Types.
Beware that this module needs Framework 4.5 and its last extensions related to HttpClient rewrite using asynch methods.
You have 2 options, either you move your full Orchard build in 4.5 (it runs perfectly, just change 4.0 to 4.5 in each project file), either you keep this sole module in 4.5 but it is less clean...

Usage:

0) For Cloud Connector Global usage, refer to our web site http://www.datwendo.com
Roughtly it is a way to generate same data type from different engines running on various Clouds or Datacenters, Orchard is one of them
- basic: integer index 
- raw text data (4K)
- blob data

1) Go to http://www.datwendo.com, register (the Git OAuth register will be available soon) then start a 15 days demo Connector or buy some months of Service.
2) collect your connector main parameters:
 - Id
 - Secret key
 - Protocol (Fast or Secure)
 3) Install the module in Orchard (until it appear in gallery, you will need to clone this repo and build it), and enable
 4) Set the site settings parameters (The demo Dawtendo Server is http://datwendosvc.cloudapp.net, the API version 1, the key delai is not important for demo connectors let it to 200)
 5) Create a Content Type using various parts and fields, or moddify an existing Content Type
 6) Add it the ClientConnector Part, fix the related connector parameters:
 - Id,
 - Secret Key
 - Protocol (always Fast for Demo), mandatory with only 2 values today: Fast/Secure
 7) Arrange the display for the ClientConnector Part in your local theme placement.info
 8) Create new Content Items -> they will automatically receive a new unique Id

 Limitation:
 This version only support the Cloud Connector without its options: Data Storage, Cloud Storage, Publish Subscribe.
 An extended version will include these options as new features in the module.

 Please Do Report any error, either here or on our site via the contact-us page


Soon on the Orchard Gallery.

Thanks
Christian Surieux
