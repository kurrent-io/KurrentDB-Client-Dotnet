<template><div><h1 id="introduction" tabindex="-1"><a class="header-anchor" href="#introduction"><span>Introduction</span></a></h1>
<p>Welcome to the KurrentDB documentation.</p>
<p>KurrentDB is a database that's engineered for modern software applications and event-driven architectures. Its event-native design simplifies data modeling and preserves data integrity while the integrated streaming engine solves distributed messaging challenges and ensures data consistency.</p>
<p>KurrentDB is licensed under <a href="https://github.com/EventStore/EventStore/blob/master/LICENSE.md" target="_blank" rel="noopener noreferrer">Kurrent License v1 (KLv1)</a>, meaning that anyone can access and use it, but enterprise features are only enabled with a valid <RouteLink to="/server/quick-start/installation.html#license-keys">license key</RouteLink>.</p>
<div class="hint-container note">
<p class="hint-container-title">Note</p>
<p>Although the source code for KurrentDB is available to view, the <a href="https://github.com/EventStore/EventStore/blob/master/LICENSE.md" target="_blank" rel="noopener noreferrer">KLv1 license</a> is not an OSI-approved Open Source License.</p>
</div>
<h2 id="what-s-new" tabindex="-1"><a class="header-anchor" href="#what-s-new"><span>What's new</span></a></h2>
<p>Find out <RouteLink to="/server/quick-start/whatsnew.html">what's new</RouteLink> in this release to get details on new features and other changes.</p>
<h2 id="getting-started" tabindex="-1"><a class="header-anchor" href="#getting-started"><span>Getting started</span></a></h2>
<p>Check the <RouteLink to="/getting-started/quickstart/">getting started guide</RouteLink> for an introduction to KurrentDB and its features, along with detailed instructions to quickly get started.</p>
<h2 id="support" tabindex="-1"><a class="header-anchor" href="#support"><span>Support</span></a></h2>
<h3 id="community" tabindex="-1"><a class="header-anchor" href="#community"><span>Community</span></a></h3>
<p>Users of KurrentDB can use the <a href="https://www.kurrent.io/community" target="_blank" rel="noopener noreferrer">community forum</a> for questions, discussions, and getting help from community members.</p>
<h3 id="enterprise-customers" tabindex="-1"><a class="header-anchor" href="#enterprise-customers"><span>Enterprise customers</span></a></h3>
<p>Customers with a paid subscription can open tickets using the <a href="https://eventstore.freshdesk.com" target="_blank" rel="noopener noreferrer">support portal</a>. For additional information on supported versions, users can access KurrentDB's <RouteLink to="/server/release-schedule/">release and support schedule</RouteLink>.</p>
<h3 id="issues" tabindex="-1"><a class="header-anchor" href="#issues"><span>Issues</span></a></h3>
<p>We openly track most issues in the KurrentDB <a href="https://github.com/EventStore/EventStore" target="_blank" rel="noopener noreferrer">repository on GitHub</a>. Before opening an issue, please ensure that a similar issue hasn't been opened already. Also, try searching closed issues that might contain a solution or workaround for your problem.</p>
<p>When opening an issue, follow our <a href="https://github.com/EventStore/EventStore/blob/master/CONTRIBUTING.md" target="_blank" rel="noopener noreferrer">guidelines</a> for bug reports and feature requests. By doing so, you will significantly help us to solve your concerns most efficiently.</p>
<h2 id="protocols-clients-and-sdks" tabindex="-1"><a class="header-anchor" href="#protocols-clients-and-sdks"><span>Protocols, clients, and SDKs</span></a></h2>
<p>KurrentDB supports one client protocol, which is described below. The older TCP client API has been removed in KurrentDB. The final version with TCP API support is EventStoreDB version 23.10. More information can be found in our <a href="https://www.kurrent.io/blog/sunsetting-eventstoredb-tcp-based-client-protocol" target="_blank" rel="noopener noreferrer">blog post</a>.</p>
<p>The legacy protocol is available for Kurrent customers as a <RouteLink to="/server/configuration/networking.html#external-tcp">licensed plugin</RouteLink>.</p>
<h3 id="client-protocol" tabindex="-1"><a class="header-anchor" href="#client-protocol"><span>Client protocol</span></a></h3>
<p>The client protocol is based on <a href="https://grpc.io/" target="_blank" rel="noopener noreferrer">open standards</a> and is widely supported by many programming languages. KurrentDB uses gRPC to communicate between the cluster nodes and for client-server communication.</p>
<p>When developing software that uses KurrentDB, we recommend using one of the official SDKs.</p>
<h4 id="supported-clients" tabindex="-1"><a class="header-anchor" href="#supported-clients"><span>Supported clients</span></a></h4>
<ul>
<li>Python: <a href="https://pypi.org/project/esdbclient/" target="_blank" rel="noopener noreferrer">pyeventsourcing/esdbclient</a></li>
<li>Node.js (JavaScript/TypeScript): <a href="https://github.com/EventStore/EventStore-Client-NodeJS" target="_blank" rel="noopener noreferrer">EventStore/EventStore-Client-NodeJS</a></li>
<li>Java: <a href="https://github.com/EventStore/EventStoreDB-Client-Java" target="_blank" rel="noopener noreferrer">(EventStore/EventStoreDB-Client-Java</a></li>
<li>.NET: <a href="https://github.com/EventStore/EventStore-Client-Dotnet" target="_blank" rel="noopener noreferrer">EventStore/EventStore-Client-Dotnet</a></li>
<li>Go: <a href="https://github.com/EventStore/EventStore-Client-Go" target="_blank" rel="noopener noreferrer">EventStore/EventStore-Client-Go</a></li>
<li>Rust: <a href="https://github.com/EventStore/EventStoreDB-Client-Rust" target="_blank" rel="noopener noreferrer">EventStore/EventStoreDB-Client-Rust</a></li>
</ul>
<p>Read more in the <RouteLink to="/server/quick-start/@clients/grpc/">gRPC clients documentation</RouteLink>.</p>
<h4 id="community-developed-clients" tabindex="-1"><a class="header-anchor" href="#community-developed-clients"><span>Community-developed clients</span></a></h4>
<ul>
<li><a href="https://github.com/yousty/event_store_client" target="_blank" rel="noopener noreferrer">Ruby (yousty/event_store_client)</a></li>
<li><a href="https://github.com/NFIBrokerage/spear" target="_blank" rel="noopener noreferrer">Elixir (NFIBrokerage/spear)</a></li>
</ul>
<h3 id="http" tabindex="-1"><a class="header-anchor" href="#http"><span>HTTP</span></a></h3>
<p>KurrentDB also offers an HTTP-based interface that consists of the REST-oriented API and a real-time subscription feature based on the <a href="https://datatracker.ietf.org/doc/html/rfc5023" target="_blank" rel="noopener noreferrer">AtomPub protocol</a>. As it operates over HTTP, this is less efficient, but nearly every environment supports it.</p>
<p>Learn more about configuring the HTTP protocol on the <RouteLink to="/server/configuration/networking.html#http-configuration">HTTP configuration</RouteLink> page.</p>
<div class="hint-container warning">
<p class="hint-container-title">Deprecation Note</p>
<p>The current AtomPub-based HTTP application API is disabled by default. You can enable it by adding an <RouteLink to="/server/configuration/networking.html#atompub">option</RouteLink> to the server configuration. Although we plan to remove AtomPub support from future server versions, the server management HTTP API will remain available.
You need to enable the AtomPub protocol to have a fully functioning database user interface.</p>
</div>
<p>Learn more about the KurrentDB HTTP interface in the <RouteLink to="/server/quick-start/@clients/http-api/">HTTP documentation</RouteLink>.</p>
<h4 id="community-developed-clients-1" tabindex="-1"><a class="header-anchor" href="#community-developed-clients-1"><span>Community-developed clients</span></a></h4>
<ul>
<li><a href="https://github.com/prooph/event-store-http-client/" target="_blank" rel="noopener noreferrer">PHP (prooph/event-store-http-client)</a></li>
<li><a href="https://github.com/yousty/event_store_client" target="_blank" rel="noopener noreferrer">Ruby (yousty/event_store_client)</a></li>
</ul>
</div></template>


