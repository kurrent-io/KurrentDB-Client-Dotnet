<template><div><h1 id="getting-started" tabindex="-1"><a class="header-anchor" href="#getting-started"><span>Getting started</span></a></h1>
<p>Get started by connecting your application to EventStoreDB.</p>
<h2 id="connecting-to-eventstoredb" tabindex="-1"><a class="header-anchor" href="#connecting-to-eventstoredb"><span>Connecting to EventStoreDB</span></a></h2>
<p>To connect your application to EventStoreDB, instantiate and configure the client.</p>
<div class="hint-container tip">
<p class="hint-container-title">Insecure clusters</p>
<p>All our GRPC clients are secure by default and must be configured to connect to an insecure server via <a href="#connection-string">a connection string</a> or the client's configuration.</p>
</div>
<h3 id="required-packages" tabindex="-1"><a class="header-anchor" href="#required-packages"><span>Required packages</span></a></h3>
<p>Add the .NET <code v-pre>EventStore.Client.Grpc</code> and <code v-pre>EventStore.Client.Grpc.Streams</code> package to your project:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">dotnet</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> add</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> package</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> EventStoreDB.Client</span></span>
<span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">dotnet</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> add</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> package</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> EventStore.Client.Grpc.Streams</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div></div></div><h3 id="connection-string" tabindex="-1"><a class="header-anchor" href="#connection-string"><span>Connection string</span></a></h3>
<p>Each SDK has its own way of configuring the client, but the connection string can always be used.
The EventStoreDB connection string supports two schemas: <code v-pre>esdb://</code> for connecting to a single-node server, and <code v-pre>esdb+discover://</code> for connecting to a multi-node cluster. The difference between the two schemas is that when using <code v-pre>esdb://</code>, the client will connect directly to the node; with <code v-pre>esdb+discover://</code> schema the client will use the gossip protocol to retrieve the cluster information and choose the right node to connect to.
Since version 22.10, ESDB supports gossip on single-node deployments, so <code v-pre>esdb+discover://</code> schema can be used for connecting to any topology.</p>
<p>The connection string has the following format:</p>
<div class="language- line-numbers-mode" data-highlighter="shiki" data-ext="" data-title="" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span>esdb+discover://admin:changeit@cluster.dns.name:2113</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>There, <code v-pre>cluster.dns.name</code> is the name of a DNS <code v-pre>A</code> record that points to all the cluster nodes. Alternatively, you can list cluster nodes separated by comma instead of the cluster DNS name:</p>
<div class="language- line-numbers-mode" data-highlighter="shiki" data-ext="" data-title="" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span>esdb+discover://admin:changeit@node1.dns.name:2113,node2.dns.name:2113,node3.dns.name:2113</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>There are a number of query parameters that can be used in the connection string to instruct the cluster how and where the connection should be established. All query parameters are optional.</p>
<table>
<thead>
<tr>
<th>Parameter</th>
<th>Accepted values</th>
<th>Default</th>
<th>Description</th>
</tr>
</thead>
<tbody>
<tr>
<td><code v-pre>tls</code></td>
<td><code v-pre>true</code>, <code v-pre>false</code></td>
<td><code v-pre>true</code></td>
<td>Use secure connection, set to <code v-pre>false</code> when connecting to a non-secure server or cluster.</td>
</tr>
<tr>
<td><code v-pre>connectionName</code></td>
<td>Any string</td>
<td>None</td>
<td>Connection name</td>
</tr>
<tr>
<td><code v-pre>maxDiscoverAttempts</code></td>
<td>Number</td>
<td><code v-pre>10</code></td>
<td>Number of attempts to discover the cluster.</td>
</tr>
<tr>
<td><code v-pre>discoveryInterval</code></td>
<td>Number</td>
<td><code v-pre>100</code></td>
<td>Cluster discovery polling interval in milliseconds.</td>
</tr>
<tr>
<td><code v-pre>gossipTimeout</code></td>
<td>Number</td>
<td><code v-pre>5</code></td>
<td>Gossip timeout in seconds, when the gossip call times out, it will be retried.</td>
</tr>
<tr>
<td><code v-pre>nodePreference</code></td>
<td><code v-pre>leader</code>, <code v-pre>follower</code>, <code v-pre>random</code>, <code v-pre>readOnlyReplica</code></td>
<td><code v-pre>leader</code></td>
<td>Preferred node role. When creating a client for write operations, always use <code v-pre>leader</code>.</td>
</tr>
<tr>
<td><code v-pre>tlsVerifyCert</code></td>
<td><code v-pre>true</code>, <code v-pre>false</code></td>
<td><code v-pre>true</code></td>
<td>In secure mode, set to <code v-pre>true</code> when using an untrusted connection to the node if you don't have the CA file available. Don't use in production.</td>
</tr>
<tr>
<td><code v-pre>tlsCaFile</code></td>
<td>String, file path</td>
<td>None</td>
<td>Path to the CA file when connecting to a secure cluster with a certificate that's not signed by a trusted CA.</td>
</tr>
<tr>
<td><code v-pre>defaultDeadline</code></td>
<td>Number</td>
<td>None</td>
<td>Default timeout for client operations, in milliseconds. Most clients allow overriding the deadline per operation.</td>
</tr>
<tr>
<td><code v-pre>keepAliveInterval</code></td>
<td>Number</td>
<td><code v-pre>10</code></td>
<td>Interval between keep-alive ping calls, in seconds.</td>
</tr>
<tr>
<td><code v-pre>keepAliveTimeout</code></td>
<td>Number</td>
<td><code v-pre>10</code></td>
<td>Keep-alive ping call timeout, in seconds.</td>
</tr>
<tr>
<td><code v-pre>userCertFile</code></td>
<td>String, file path</td>
<td>None</td>
<td>User certificate file for X.509 authentication.</td>
</tr>
<tr>
<td><code v-pre>userKeyFile</code></td>
<td>String, file path</td>
<td>None</td>
<td>Key file for the user certificate used for X.509 authentication.</td>
</tr>
</tbody>
</table>
<p>When connecting to an insecure instance, specify <code v-pre>tls=false</code> parameter. For example, for a node running locally use <code v-pre>esdb://localhost:2113?tls=false</code>. Note that usernames and passwords aren't provided there because insecure deployments don't support authentication and authorisation.</p>
<h3 id="creating-a-client" tabindex="-1"><a class="header-anchor" href="#creating-a-client"><span>Creating a client</span></a></h3>
<p>First, create a client and get it connected to the database.</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> client</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">EventStoreClient</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">EventStoreClientSettings</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">Create</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"esdb://admin:changeit@localhost:2113?tls=false&#x26;tlsVerifyCert=false"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">));</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>The client instance can be used as a singleton across the whole application. It doesn't need to open or close the connection.</p>
<h3 id="creating-an-event" tabindex="-1"><a class="header-anchor" href="#creating-an-event"><span>Creating an event</span></a></h3>
<p>You can write anything to EventStoreDB as events. The client needs a byte array as the event payload. Normally, you'd use a serialized object, and it's up to you to choose the serialization method.</p>
<div class="hint-container tip">
<p class="hint-container-title">Server-side projections</p>
<p>User-defined server-side projections require events to be serialized in JSON format.</p>
<p>We use JSON for serialization in the documentation examples.</p>
</div>
<p>The code snippet below creates an event object instance, serializes it, and adds it as a payload to the <code v-pre>EventData</code> structure, which the client can then write to the database.</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">using</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B"> System</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">Text</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">Json</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">;</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> evt</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">TestEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> {</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">	EntityId</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">      =</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> Guid</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">NewGuid</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">().</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">ToString</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"N"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">),</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">	ImportantData</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> "I wrote my first event!"</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">};</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> eventData</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">EventData</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">	Uuid</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">NewUuid</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(),</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">	"TestEvent"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">	JsonSerializer</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">SerializeToUtf8Bytes</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">evt</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">)</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><h3 id="appending-events" tabindex="-1"><a class="header-anchor" href="#appending-events"><span>Appending events</span></a></h3>
<p>Each event in the database has its own unique identifier (UUID). The database uses it to ensure idempotent writes, but it only works if you specify the stream revision when appending events to the stream.</p>
<p>In the snippet below, we append the event to the stream <code v-pre>some-stream</code>.</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">AppendToStreamAsync</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">	"some-stream"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">	StreamState</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Any</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">	new[] { </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">eventData</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> },</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">	cancellationToken</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">cancellationToken</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><p>Here we are appending events without checking if the stream exists or if the stream version matches the expected event version. See more advanced scenarios in <RouteLink to="/api/appending-events.html">appending events documentation</RouteLink>.</p>
<h3 id="reading-events" tabindex="-1"><a class="header-anchor" href="#reading-events"><span>Reading events</span></a></h3>
<p>Finally, we can read events back from the <code v-pre>some-stream</code> stream.</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> result</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">ReadStreamAsync</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">	Direction</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Forwards</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">	"some-stream"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">	StreamPosition</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Start</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">	cancellationToken</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">cancellationToken</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> events</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">result</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">ToListAsync</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">cancellationToken</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><p>When you read events from the stream, you get a collection of <code v-pre>ResolvedEvent</code> structures. The event payload is returned as a byte array and needs to be deserialized. See more advanced scenarios in <RouteLink to="/api/reading-events.html">reading events documentation</RouteLink>.</p>
</div></template>


