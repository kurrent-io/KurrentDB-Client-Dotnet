Connection string
Each SDK has its own way of configuring the client, but the connection string can always be used. The KurrentDB connection string supports two schemas: kurrentdb:// for connecting to a single-node server, and kurrentdb+discover:// for connecting to a multi-node cluster. The difference between the two schemas is that when using kurrentdb://, the client will connect directly to the node; with kurrentdb+discover:// schema the client will use the gossip protocol to retrieve the cluster information and choose the right node to connect to. Since version 22.10, ESDB supports gossip on single-node deployments, so kurrentdb+discover:// schema can be used for connecting to any topology.

The connection string has the following format:


kurrentdb+discover://admin:changeit@cluster.dns.name:2113
There, cluster.dns.name is the name of a DNS A record that points to all the cluster nodes. Alternatively, you can list cluster nodes separated by comma instead of the cluster DNS name:


kurrentdb+discover://admin:changeit@node1.dns.name:2113,node2.dns.name:2113,node3.dns.name:2113
There are a number of query parameters that can be used in the connection string to instruct the cluster how and where the connection should be established. All query parameters are optional.

Parameter	Accepted values	Default	Description
tls	true, false	true	Use secure connection, set to false when connecting to a non-secure server or cluster.
connectionName	Any string	None	Connection name
maxDiscoverAttempts	Number	10	Number of attempts to discover the cluster.
discoveryInterval	Number	100	Cluster discovery polling interval in milliseconds.
gossipTimeout	Number	5	Gossip timeout in seconds, when the gossip call times out, it will be retried.
nodePreference	leader, follower, random, readOnlyReplica	leader	Preferred node role. When creating a client for write operations, always use leader.
tlsVerifyCert	true, false	true	In secure mode, set to true when using an untrusted connection to the node if you don't have the CA file available. Don't use in production.
tlsCaFile	String, file path	None	Path to the CA file when connecting to a secure cluster with a certificate that's not signed by a trusted CA.
defaultDeadline	Number	None	Default timeout for client operations, in milliseconds. Most clients allow overriding the deadline per operation.
keepAliveInterval	Number	10	Interval between keep-alive ping calls, in seconds.
keepAliveTimeout	Number	10	Keep-alive ping call timeout, in seconds.
userCertFile	String, file path	None	User certificate file for X.509 authentication.
userKeyFile	String, file path	None	Key file for the user certificate used for X.509 authentication.
When connecting to an insecure instance, specify tls=false parameter. For example, for a node running locally use kurrentdb://localhost:2113?tls=false. Note that usernames and passwords aren't provided there because insecure deployments don't support authentication and authorisation.
