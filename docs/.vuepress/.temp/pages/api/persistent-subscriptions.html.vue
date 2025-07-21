<template><div><h1 id="persistent-subscriptions" tabindex="-1"><a class="header-anchor" href="#persistent-subscriptions"><span>Persistent Subscriptions</span></a></h1>
<p>Persistent subscriptions are similar to catch-up subscriptions, but there are two key differences:</p>
<ul>
<li>The subscription checkpoint is maintained by the server. It means that when your client reconnects to the persistent subscription, it will automatically resume from the last known position.</li>
<li>It's possible to connect more than one event consumer to the same persistent subscription. In that case, the server will load-balance the consumers, depending on the defined strategy, and distribute the events to them.</li>
</ul>
<p>Because of those, persistent subscriptions are defined as subscription groups that are defined and maintained by the server. Consumer then connect to a particular subscription group, and the server starts sending event to the consumer.</p>
<p>You can read more about persistent subscriptions in the <RouteLink to="/server/features/persistent-subscriptions.html">server documentation</RouteLink>.</p>
<h2 id="required-packages" tabindex="-1"><a class="header-anchor" href="#required-packages"><span>Required packages</span></a></h2>
<p>Add the .NET <code v-pre>EventStore.Client.Grpc</code> and <code v-pre>EventStore.Client.Grpc.PersistentSubscriptions</code> package to your project:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">dotnet</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> add</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> package</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> EventStoreDB.Client</span></span>
<span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">dotnet</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> add</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> package</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> EventStore.Client.Grpc.PersistentSubscriptions</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div></div></div><h2 id="creating-a-subscription-group" tabindex="-1"><a class="header-anchor" href="#creating-a-subscription-group"><span>Creating a subscription group</span></a></h2>
<p>The first step of dealing with a persistent subscription is to create a subscription group. You will receive an error if you attempt to create a subscription group multiple times. You must have admin permissions to create a persistent subscription group.</p>
<h3 id="subscribing-to-one-stream" tabindex="-1"><a class="header-anchor" href="#subscribing-to-one-stream"><span>Subscribing to one stream</span></a></h3>
<p>The following sample shows how to create a subscription group for a persistent subscription where you want to receive events from a specific stream. It could be a normal stream, or a stream of links (like <code v-pre>$ce</code> category stream).</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">UserCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"admin"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"changeit"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> settings</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">PersistentSubscriptionSettings</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">();</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">CreateToStreamAsync</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  "test-stream"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  "subscription-group"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">  settings</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">  userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">userCredentials</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Console</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">WriteLine</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"Subscription to stream created"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><table>
<thead>
<tr>
<th style="text-align:left">Parameter</th>
<th style="text-align:left">Description</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left"><code v-pre>stream</code></td>
<td style="text-align:left">The stream the persistent subscription is on.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>groupName</code></td>
<td style="text-align:left">The name of the subscription group to create.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>settings</code></td>
<td style="text-align:left">The settings to use when creating the subscription.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>credentials</code></td>
<td style="text-align:left">The user credentials to use for this operation.</td>
</tr>
</tbody>
</table>
<h3 id="subscribing-to-all" tabindex="-1"><a class="header-anchor" href="#subscribing-to-all"><span>Subscribing to $all</span></a></h3>
<p>The ability to subscribe to <code v-pre>$all</code> was introduced in EventStoreDB <strong>21.10</strong>. Persistent subscriptions to <code v-pre>$all</code> also support <RouteLink to="/api/subscriptions.html#server-side-filtering">filtering</RouteLink>.</p>
<p>You can create a subscription group on $all much the same way you would create a subscription group on a stream:</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">UserCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"admin"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"changeit"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> filter</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> StreamFilter</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">Prefix</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"test"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> settings</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">PersistentSubscriptionSettings</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">();</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">CreateToAllAsync</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  "subscription-group"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">  filter</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">  settings</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">  userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">userCredentials</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><h2 id="connecting-a-consumer" tabindex="-1"><a class="header-anchor" href="#connecting-a-consumer"><span>Connecting a consumer</span></a></h2>
<p>Once you have created a subscription group, clients can connect to it. A subscription in your application should only have the connection in your code, you should assume that the subscription already exists.</p>
<p>The most important parameter to pass when connecting is the buffer size. This represents how many outstanding messages the server should allow this client. If this number is too small, your subscription will spend much of its time idle as it waits for an acknowledgment to come back from the client. If it's too big, you waste resources and can start causing time out messages depending on the speed of your processing.</p>
<h3 id="connecting-to-one-stream" tabindex="-1"><a class="header-anchor" href="#connecting-to-one-stream"><span>Connecting to one stream</span></a></h3>
<p>The code below shows how to connect to an existing subscription group for a specific stream:</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">using</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD"> var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">SubscribeToStream</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"test-stream"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"subscription-group"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">cancellationToken</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">ct</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">foreach</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> message</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD"> in</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Messages</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">  switch</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">message</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">    case</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B"> PersistentSubscriptionMessage</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">SubscriptionConfirmation</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> subscriptionId</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">):</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">      Console</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">WriteLine</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">$"Subscription {</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">subscriptionId</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">} to stream started"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">      break</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">;</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">    case</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B"> PersistentSubscriptionMessage</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">Event</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#E45649;--shiki-dark:#E5C07B">_</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">):</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">      await </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">HandleEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">      await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">Ack</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">      break</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">;</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  }</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">}</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><table>
<thead>
<tr>
<th style="text-align:left">Parameter</th>
<th style="text-align:left">Description</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left"><code v-pre>stream</code></td>
<td style="text-align:left">The stream the persistent subscription is on.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>groupName</code></td>
<td style="text-align:left">The name of the subscription group to subscribe to.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>eventAppeared</code></td>
<td style="text-align:left">The action to call when an event arrives over the subscription.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>subscriptionDropped</code></td>
<td style="text-align:left">The action to call if the subscription is dropped.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>credentials</code></td>
<td style="text-align:left">The user credentials to use for this operation.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>bufferSize</code></td>
<td style="text-align:left">The number of in-flight messages this client is allowed. <strong>Default: 10</strong></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>autoAck</code></td>
<td style="text-align:left">Whether to automatically acknowledge messages after eventAppeared returns. <strong>Default: true</strong></td>
</tr>
</tbody>
</table>
<div class="hint-container warning">
<p class="hint-container-title">Warning</p>
<p>The <code v-pre>autoAck</code> parameter will be deprecated in the next client release. You'll need to explicitly <a href="#acknowledgements">manage acknowledgements</a>.</p>
</div>
<h3 id="connecting-to-all" tabindex="-1"><a class="header-anchor" href="#connecting-to-all"><span>Connecting to $all</span></a></h3>
<p>The code below shows how to connect to an existing subscription group for <code v-pre>$all</code>:</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">using</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD"> var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">SubscribeToAll</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"subscription-group"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">cancellationToken</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">ct</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">foreach</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> message</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD"> in</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Messages</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">  switch</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">message</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">    case</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B"> PersistentSubscriptionMessage</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">SubscriptionConfirmation</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> subscriptionId</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">):</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">      Console</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">WriteLine</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">$"Subscription {</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">subscriptionId</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">} to stream started"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">      break</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">;</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">    case</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B"> PersistentSubscriptionMessage</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">Event</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#E45649;--shiki-dark:#E5C07B">_</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">):</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">      await </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">HandleEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">      break</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">;</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  }</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">}</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><p>The <code v-pre>SubscribeToAllAsync</code> method is identical to the <code v-pre>SubscribeToStreamAsync</code> method, except that you don't need to specify a stream name.</p>
<h2 id="acknowledgements" tabindex="-1"><a class="header-anchor" href="#acknowledgements"><span>Acknowledgements</span></a></h2>
<p>Clients must acknowledge (or not acknowledge) messages in the competing consumer model.</p>
<p>If processing is successful, you must send an Ack (acknowledge) to the server to let it know that the message has been handled. If processing fails for some reason, then you can Nack (not acknowledge) the message and tell the server how to handle the failure.</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">using</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD"> var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">SubscribeToStream</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  "test-stream"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  "subscription-group"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">  cancellationToken</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">ct</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">foreach</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> message</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD"> in</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B"> subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Messages</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">  switch</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">message</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">    case</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B"> PersistentSubscriptionMessage</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">SubscriptionConfirmation</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> subscriptionId</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">):</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">      Console</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">WriteLine</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">$"Subscription {</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">subscriptionId</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">} to stream with manual acks started"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">      break</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">;</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">    case</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B"> PersistentSubscriptionMessage</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">Event</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#E45649;--shiki-dark:#E5C07B">_</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">):</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">      try</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> {</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">        await </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">HandleEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">        await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">Ack</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">      } </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">catch</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">UnrecoverableException</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> ex</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">        await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">subscription</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">Nack</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">PersistentSubscriptionNakEventAction</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Park</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">ex</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Message</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">resolvedEvent</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">      }</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">      break</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">;</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  }</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">}</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><p>The <em>Nack event action</em> describes what the server should do with the message:</p>
<table>
<thead>
<tr>
<th style="text-align:left">Action</th>
<th style="text-align:left">Description</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left"><code v-pre>Unknown</code></td>
<td style="text-align:left">The client does not know what action to take. Let the server decide.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>Park</code></td>
<td style="text-align:left">Park the message and do not resend. Put it on poison queue.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>Retry</code></td>
<td style="text-align:left">Explicitly retry the message.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>Skip</code></td>
<td style="text-align:left">Skip this message do not resend and do not put in poison queue.</td>
</tr>
</tbody>
</table>
<h2 id="consumer-strategies" tabindex="-1"><a class="header-anchor" href="#consumer-strategies"><span>Consumer strategies</span></a></h2>
<p>When creating a persistent subscription, you can choose between a number of consumer strategies.</p>
<h3 id="roundrobin-default" tabindex="-1"><a class="header-anchor" href="#roundrobin-default"><span>RoundRobin (default)</span></a></h3>
<p>Distributes events to all clients evenly. If the client <code v-pre>bufferSize</code> is reached, the client won't receive more events until it acknowledges or not acknowledges events in its buffer.</p>
<p>This strategy provides equal load balancing between all consumers in the group.</p>
<h3 id="dispatchtosingle" tabindex="-1"><a class="header-anchor" href="#dispatchtosingle"><span>DispatchToSingle</span></a></h3>
<p>Distributes events to a single client until the <code v-pre>bufferSize</code> is reached. After that, the next client is selected in a round-robin style, and the process repeats.</p>
<p>This option can be seen as a fall-back scenario for high availability, when a single consumer processes all the events until it reaches its maximum capacity. When that happens, another consumer takes the load to free up the main consumer resources.</p>
<h3 id="pinned" tabindex="-1"><a class="header-anchor" href="#pinned"><span>Pinned</span></a></h3>
<p>For use with an indexing projection such as the system <code v-pre>$by_category</code> projection.</p>
<p>KurrentDB inspects the event for its source stream id, hashing the id to one of 1024 buckets assigned to individual clients. When a client disconnects, its buckets are assigned to other clients. When a client connects, it is assigned some existing buckets. This naively attempts to maintain a balanced workload.</p>
<p>The main aim of this strategy is to decrease the likelihood of concurrency and ordering issues while maintaining load balancing. This is <strong>not a guarantee</strong>, and you should handle the usual ordering and concurrency issues.</p>
<h2 id="updating-a-subscription-group" tabindex="-1"><a class="header-anchor" href="#updating-a-subscription-group"><span>Updating a subscription group</span></a></h2>
<p>You can edit the settings of an existing subscription group while it is running, you don't need to delete and recreate it to change settings. When you update the subscription group, it resets itself internally, dropping the connections and having them reconnect. You must have admin permissions to update a persistent subscription group.</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">UserCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"admin"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"changeit"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> settings</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">PersistentSubscriptionSettings</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">true</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">checkPointLowerBound</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">20</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">UpdateToStreamAsync</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  "test-stream"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  "subscription-group"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">  settings</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">  userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">userCredentials</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Console</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">WriteLine</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"Subscription updated"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><table>
<thead>
<tr>
<th style="text-align:left">Parameter</th>
<th style="text-align:left">Description</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left"><code v-pre>stream</code></td>
<td style="text-align:left">The stream the persistent subscription is on.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>groupName</code></td>
<td style="text-align:left">The name of the subscription group to update.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>settings</code></td>
<td style="text-align:left">The settings to use when creating the subscription.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>credentials</code></td>
<td style="text-align:left">The user credentials to use for this operation.</td>
</tr>
</tbody>
</table>
<h2 id="persistent-subscription-settings" tabindex="-1"><a class="header-anchor" href="#persistent-subscription-settings"><span>Persistent subscription settings</span></a></h2>
<p>Both the <code v-pre>Create</code> and <code v-pre>Update</code> methods take some settings for configuring the persistent subscription.</p>
<p>The following table shows the configuration options you can set on a persistent subscription.</p>
<table>
<thead>
<tr>
<th style="text-align:left">Option</th>
<th style="text-align:left">Description</th>
<th style="text-align:left">Default</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left"><code v-pre>ResolveLinkTos</code></td>
<td style="text-align:left">Whether the subscription should resolve link events to their linked events.</td>
<td style="text-align:left"><code v-pre>false</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>StartFrom</code></td>
<td style="text-align:left">The exclusive position in the stream or transaction file the subscription should start from.</td>
<td style="text-align:left"><code v-pre>null</code> (start from the end of the stream)</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>ExtraStatistics</code></td>
<td style="text-align:left">Whether to track latency statistics on this subscription.</td>
<td style="text-align:left"><code v-pre>false</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>MessageTimeout</code></td>
<td style="text-align:left">The amount of time after which to consider a message as timed out and retried.</td>
<td style="text-align:left"><code v-pre>30</code> (seconds)</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>MaxRetryCount</code></td>
<td style="text-align:left">The maximum number of retries (due to timeout) before a message is considered to be parked.</td>
<td style="text-align:left"><code v-pre>10</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>LiveBufferSize</code></td>
<td style="text-align:left">The size of the buffer (in-memory) listening to live messages as they happen before paging occurs.</td>
<td style="text-align:left"><code v-pre>500</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>ReadBatchSize</code></td>
<td style="text-align:left">The number of events read at a time when paging through history.</td>
<td style="text-align:left"><code v-pre>20</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>HistoryBufferSize</code></td>
<td style="text-align:left">The number of events to cache when paging through history.</td>
<td style="text-align:left"><code v-pre>500</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>CheckPointAfter</code></td>
<td style="text-align:left">The amount of time to try to checkpoint after.</td>
<td style="text-align:left"><code v-pre>2</code> seconds</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>MinCheckPointCount</code></td>
<td style="text-align:left">The minimum number of messages to process before a checkpoint may be written.</td>
<td style="text-align:left"><code v-pre>10</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>MaxCheckPointCount</code></td>
<td style="text-align:left">The maximum number of messages not checkpointed before forcing a checkpoint.</td>
<td style="text-align:left"><code v-pre>1000</code></td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>MaxSubscriberCount</code></td>
<td style="text-align:left">The maximum number of subscribers allowed.</td>
<td style="text-align:left"><code v-pre>0</code> (unbounded)</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>NamedConsumerStrategy</code></td>
<td style="text-align:left">The strategy to use for distributing events to client consumers. See the <a href="#consumer-strategies">consumer strategies</a> in this doc.</td>
<td style="text-align:left"><code v-pre>RoundRobin</code></td>
</tr>
</tbody>
</table>
<h2 id="deleting-a-subscription-group" tabindex="-1"><a class="header-anchor" href="#deleting-a-subscription-group"><span>Deleting a subscription group</span></a></h2>
<p>Remove a subscription group with the delete operation. Like the creation of groups, you rarely do this in your runtime code and is undertaken by an administrator running a script.</p>
<div class="language-cs line-numbers-mode" data-highlighter="shiki" data-ext="cs" data-title="cs" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">try</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> {</span></span>
<span class="line"><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">  var</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> =</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> new </span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">UserCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"admin"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">, </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"changeit"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  await </span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">client</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">DeleteToStreamAsync</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">    "test-stream"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">    "subscription-group"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">,</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">    userCredentials</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75">userCredentials</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  );</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">  Console</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">WriteLine</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"Subscription to stream deleted"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">} </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">catch</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">PersistentSubscriptionNotFoundException</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#A0A1A7;--shiki-light-font-style:italic;--shiki-dark:#7F848E;--shiki-dark-font-style:italic">  // ignore</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">} </span><span style="--shiki-light:#A626A4;--shiki-dark:#C678DD">catch</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> (</span><span style="--shiki-light:#C18401;--shiki-dark:#E5C07B">Exception</span><span style="--shiki-light:#383A42;--shiki-dark:#E06C75"> ex</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">) {</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">  Console</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">WriteLine</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">(</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">$"Subscription to stream delete error: {</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">ex</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">.</span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">GetType</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">()} {</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">ex</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">.</span><span style="--shiki-light:#383A42;--shiki-dark:#E5C07B">Message</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">}"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">);</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">}</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><table>
<thead>
<tr>
<th style="text-align:left">Parameter</th>
<th style="text-align:left">Description</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left"><code v-pre>stream</code></td>
<td style="text-align:left">The stream the persistent subscription is on.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>groupName</code></td>
<td style="text-align:left">The name of the subscription group to delete.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>credentials</code></td>
<td style="text-align:left">The user credentials to use for this operation</td>
</tr>
</tbody>
</table>
</div></template>


