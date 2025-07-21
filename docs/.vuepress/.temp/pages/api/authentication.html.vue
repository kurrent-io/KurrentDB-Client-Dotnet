<template><div><h2 id="client-x-509-certificate" tabindex="-1"><a class="header-anchor" href="#client-x-509-certificate"><span>Client x.509 certificate <Badge type="warning" text="Commercial" vertical="middle"/></span></a></h2>
<p>X.509 certificates are digital certificates that use the X.509 public key infrastructure (PKI) standard to verify the identity of clients and servers. They play a crucial role in establishing a secure connection by providing a way to authenticate identities and establish trust.</p>
<h3 id="prerequisites" tabindex="-1"><a class="header-anchor" href="#prerequisites"><span>Prerequisites</span></a></h3>
<ol>
<li>KurrentDB 25.0 or greater, or EventStoreDB 24.10.</li>
<li>A commercial license with the User Certificates entitlement.</li>
<li>A valid x.509 certificate, which can be created using version <code v-pre>1.3</code> or higher of the <a href="https://github.com/kurrent-io/es-gencert-cli" target="_blank" rel="noopener noreferrer">gencert tool</a>.</li>
<li>The server must run in secure mode. See <RouteLink to="/server/security/protocol-security.html">Security Options</RouteLink> for more information.</li>
<li><RouteLink to="/server/security/user-authentication.html#user-x509-certificates">Enable User Certificates plugin on the server</RouteLink></li>
</ol>
<h4 id="generate-user-certificates" tabindex="-1"><a class="header-anchor" href="#generate-user-certificates"><span>Generate user certificates</span></a></h4>
<p>The following command uses the <a href="https://github.com/kurrent-io/es-gencert-cli" target="_blank" rel="noopener noreferrer">gencert tool</a> to generate a user certificate for the user <code v-pre>admin</code> that will expire in 10 days:</p>
<Tabs id="42" :data='[{"id":"bash"},{"id":"PowerShell"}]' tab-id="os">
<template #title0="{ value, isActive }">bash</template>
<template #title1="{ value, isActive }">PowerShell</template>
<template #tab0="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">./es-gencert-cli</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> create-user</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -username</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> admin</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -days</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> 10</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -ca-certificate</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> ./es-ca/ca.crt</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -ca-key</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> ./es-ca/ca.key</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div></template>
<template #tab1="{ value, isActive }">
<div class="language-powershell line-numbers-mode" data-highlighter="shiki" data-ext="powershell" data-title="powershell" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.\</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2">es-gencert-cli.exe</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> create</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">user </span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">username admin </span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">days </span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">10</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> -</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">ca</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">certificate .</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">es</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">ca</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">ca.crt </span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">ca</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">key .</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">es</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">ca</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">ca.key</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div></template>
</Tabs>
<h3 id="connect-to-kurrentdb-using-an-x-509-certificate" tabindex="-1"><a class="header-anchor" href="#connect-to-kurrentdb-using-an-x-509-certificate"><span>Connect to KurrentDB using an x.509 certificate</span></a></h3>
<p>To connect to KurrentDB using an x.509 certificate, you need to provide the
certificate and the private key to the client. If both username/password and
certificate authentication data are supplied, the client prioritizes user
credentials for authentication. The client will throw an error if the
certificate and the key are not both provided.</p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>Please note that currently, password-protected private key files are not supported.</p>
</div>
<p>The client supports the following parameters:</p>
<table>
<thead>
<tr>
<th>Parameter</th>
<th>Description</th>
</tr>
</thead>
<tbody>
<tr>
<td><code v-pre>userCertFile</code></td>
<td>The file containing the X.509 user certificate in PEM format.</td>
</tr>
<tr>
<td><code v-pre>userKeyFile</code></td>
<td>The file containing the user certificate’s matching private key in PEM format.</td>
</tr>
</tbody>
</table>
<p>To authenticate, include these two parameters in your connection string or constructor when initializing the client.</p>
<p>Check the samples for the following clients:</p>
<p>@<a href="@grpc:user-certificates/Program.cs">code{client-with-user-certificates}</a></p>
</div></template>


