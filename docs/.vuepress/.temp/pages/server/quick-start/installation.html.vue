<template><div><h2 id="quick-start" tabindex="-1"><a class="header-anchor" href="#quick-start"><span>Quick start</span></a></h2>
<p>KurrentDB can run as a single node or as a highly-available cluster. For the cluster deployment, you'd need three server nodes.</p>
<p>The installation procedure consists of the following steps:</p>
<ul>
<li>Create a configuration file for each cluster node. If you are using any licensed features, ensure that you configure a <a href="#license-keys">license key</a>.</li>
<li>Install KurrentDB on each node using one of the available methods.</li>
<li>Obtain SSL certificates, either signed by a publicly trusted or private certificate authority.</li>
<li>Copy the configuration files and SSL certificates to each node.</li>
<li>Start the KurrentDB service on each node.</li>
<li>Check the cluster status using the Admin UI on any node.</li>
</ul>
<h3 id="default-access" tabindex="-1"><a class="header-anchor" href="#default-access"><span>Default access</span></a></h3>
<table>
<thead>
<tr>
<th>User</th>
<th>Password</th>
</tr>
</thead>
<tbody>
<tr>
<td>admin</td>
<td>changeit</td>
</tr>
<tr>
<td>ops</td>
<td>changeit</td>
</tr>
</tbody>
</table>
<h3 id="license-keys" tabindex="-1"><a class="header-anchor" href="#license-keys"><span>License Keys</span></a></h3>
<p>Some features of KurrentDB require a license key to access. When you purchase an enterprise subscription, the license key will be sent to your company's designated license administrator. Existing customers who would like to upgrade to a 24.10+ enterprise license should contact their Kurrent (formerly Event Store) account manager or contact us <a href="https://www.kurrent.io/talk_to_expert" target="_blank" rel="noopener noreferrer">here</a>. As an existing customer, you can also try the enterprise features by signing up for a <a href="https://www.kurrent.io/kurrent_free_trial" target="_blank" rel="noopener noreferrer">free trial license key</a>.</p>
<p>There are various ways to provide the license key to KurrentDB. For more information, refer to the <RouteLink to="/server/configuration/">configuration guide</RouteLink>.</p>
<p>Configuration file:</p>
<div class="language-yaml line-numbers-mode" data-highlighter="shiki" data-ext="yaml" data-title="yaml" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#E45649;--shiki-dark:#E06C75">Licensing</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">:</span></span>
<span class="line"><span style="--shiki-light:#E45649;--shiki-dark:#E06C75">  LicenseKey</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">: </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">Yourkey</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div></div></div><p>Environment variable:</p>
<div class="language- line-numbers-mode" data-highlighter="shiki" data-ext="" data-title="" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span>KURRENTDB_LICENSING__LICENSE_KEY</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>For most features that require a license, KurrentDB will not start if the feature is enabled but the license key is not provided or is invalid.</p>
<h2 id="package-repositories" tabindex="-1"><a class="header-anchor" href="#package-repositories"><span>Package repositories</span></a></h2>
<p>Packages for KurrentDB are hosted on <a href="https://cloudsmith.io/~eventstore" target="_blank" rel="noopener noreferrer">Cloudsmith</a>, in the following repositories:</p>
<!--* [kurrent-lts](https://cloudsmith.io/~eventstore/repos/kurrent-lts) containing only production-ready [LTS](../release-schedule/#long-term-support-releases) packages.-->
<ul>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-latest" target="_blank" rel="noopener noreferrer">kurrent-latest</a> containing production-ready LTS and <RouteLink to="/server/release-schedule/#short-term-support-releases">STS</RouteLink> packages.</li>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-preview" target="_blank" rel="noopener noreferrer">kurrent-preview</a> containing non-production preview packages.</li>
</ul>
<h2 id="linux" tabindex="-1"><a class="header-anchor" href="#linux"><span>Linux</span></a></h2>
<p>KurrentDB has pre-built packages available on Cloudsmith for RedHat or Debian-based distributions.
The name of the KurrentDB package is <code v-pre>kurrentdb</code>.</p>
<h3 id="ubuntu-debian-packages" tabindex="-1"><a class="header-anchor" href="#ubuntu-debian-packages"><span>Ubuntu/Debian packages</span></a></h3>
<p>Debian packages can be found in the following repositories:</p>
<!--* [kurrent-lts](https://cloudsmith.io/~eventstore/repos/kurrent-lts/packages/?q=format%3Adeb+name%3Akurrentdb) containing only production-ready [LTS](../release-schedule/#long-term-support-releases) packages.-->
<ul>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-latest/packages/?q=format%3Adeb+name%3Akurrentdb" target="_blank" rel="noopener noreferrer">kurrent-latest</a> containing production-ready LTS and <RouteLink to="/server/release-schedule/#short-term-support-releases">STS</RouteLink> packages.</li>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-preview/packages/?q=format%3Adeb+name%3Akurrentdb" target="_blank" rel="noopener noreferrer">kurrent-preview</a> containing non-production preview packages.</li>
</ul>
<h4 id="distribution-setup" tabindex="-1"><a class="header-anchor" href="#distribution-setup"><span>Distribution setup</span></a></h4>
<p>To install packages, you can quickly set up the repository automatically (recommended):</p>
<Tabs id="144" :data='[{"id":"kurrent-latest"},{"id":"kurrent-preview"}]'>
<template #title0="{ value, isActive }">kurrent-latest</template>
<template #title1="{ value, isActive }">kurrent-preview</template>
<template #tab0="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-latest/setup.deb.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><!--@tab kurrent-lts
```bash
curl -1sLf \
  'https://packages.kurrent.io/public/kurrent-lts/setup.deb.sh' \
  | sudo -E bash
```-->
</template>
<template #tab1="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-preview/setup.deb.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div></template>
</Tabs>
<p>If you need to force a specific distribution, release/version, architecture, or component (if supported), you can also do that (e.g. if your system is compatible but not identical):</p>
<Tabs id="156" :data='[{"id":"kurrent-latest"},{"id":"kurrent-preview"}]'>
<template #title0="{ value, isActive }">kurrent-latest</template>
<template #title1="{ value, isActive }">kurrent-preview</template>
<template #tab0="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-latest/setup.deb.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> distro=DISTRO</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> codename=CODENAME</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> arch=ARCH</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> component=COMPONENT</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><!--@tab kurrent-lts
```bash
curl -1sLf \
  'https://packages.kurrent.io/public/kurrent-lts/setup.deb.sh' \
  | sudo -E distro=DISTRO codename=CODENAME arch=ARCH component=COMPONENT bash
```-->
</template>
<template #tab1="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-preview/setup.deb.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> distro=DISTRO</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> codename=CODENAME</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> arch=ARCH</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> component=COMPONENT</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div></template>
</Tabs>
<p>Alternatively, you can find instructions to manually configure it yourself on Cloudsmith:</p>
<!--* [kurrent-lts](https://cloudsmith.io/~eventstore/repos/kurrent-lts/setup/#formats-deb)-->
<ul>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-latest/setup/#formats-deb" target="_blank" rel="noopener noreferrer">kurrent-latest</a></li>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-preview/setup/#formats-deb" target="_blank" rel="noopener noreferrer">kurrent-preview</a></li>
</ul>
<h4 id="install-with-apt-get" tabindex="-1"><a class="header-anchor" href="#install-with-apt-get"><span>Install with apt-get</span></a></h4>
<p>Install the package:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">apt-get</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> install</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> kurrentdb=</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">25.0.0</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><h4 id="uninstall-with-apt-get" tabindex="-1"><a class="header-anchor" href="#uninstall-with-apt-get"><span>Uninstall with apt-get</span></a></h4>
<p>You can uninstall the package with:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">apt-get</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> remove</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> kurrentdb</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>If you want to also remove any configuration files and user settings, use:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">apt-get</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> purge</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> kurrentdb</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><h3 id="redhat-packages" tabindex="-1"><a class="header-anchor" href="#redhat-packages"><span>RedHat packages</span></a></h3>
<p>RedHat packages can be found in the following repositories:</p>
<!--* [kurrent-lts](https://cloudsmith.io/~eventstore/repos/kurrent-lts/packages/?q=format%3Arpm+name%3Akurrentdb) containing only production-ready [LTS](../release-schedule/#long-term-support-releases) packages.-->
<ul>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-latest/packages/?q=format%3Arpm+name%3Akurrentdb" target="_blank" rel="noopener noreferrer">kurrent-latest</a> containing production-ready LTS and <RouteLink to="/server/release-schedule/#short-term-support-releases">STS</RouteLink> packages.</li>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-preview/packages/?q=format%3Arpm+name%3Akurrentdb" target="_blank" rel="noopener noreferrer">kurrent-preview</a> containing non-production preview packages.</li>
</ul>
<h4 id="distribution-setup-1" tabindex="-1"><a class="header-anchor" href="#distribution-setup-1"><span>Distribution setup</span></a></h4>
<p>To install packages, you can quickly set up the repository automatically (recommended):</p>
<Tabs id="224" :data='[{"id":"kurrent-latest"},{"id":"kurrent-preview"}]'>
<template #title0="{ value, isActive }">kurrent-latest</template>
<template #title1="{ value, isActive }">kurrent-preview</template>
<template #tab0="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-latest/setup.rpm.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><!--@tab kurrent-lts
```bash
curl -1sLf \
  'https://packages.kurrent.io/public/kurrent-lts/setup.rpm.sh' \
  | sudo -E bash
```-->
</template>
<template #tab1="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-preview/setup.rpm.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div></template>
</Tabs>
<p>If you need to force a specific distribution, release/version, or architecture, you can also do that (e.g. if your system is compatible but not identical):</p>
<Tabs id="236" :data='[{"id":"kurrent-latest"},{"id":"kurrent-preview"}]'>
<template #title0="{ value, isActive }">kurrent-latest</template>
<template #title1="{ value, isActive }">kurrent-preview</template>
<template #tab0="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-latest/setup.rpm.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> distro=DISTRO</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> codename=CODENAME</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> arch=ARCH</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><!--@tab kurrent-lts
```bash
curl -1sLf \
  'https://packages.kurrent.io/public/kurrent-lts/setup.rpm.sh' \
  | sudo -E distro=DISTRO codename=CODENAME arch=ARCH bash
```-->
</template>
<template #tab1="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">curl</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -1sLf</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">  'https://packages.kurrent.io/public/kurrent-preview/setup.rpm.sh'</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  | </span><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">sudo</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -E</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> distro=DISTRO</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> codename=CODENAME</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> arch=ARCH</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> bash</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div></template>
</Tabs>
<p>Alternatively, you can find instructions to manually configure it yourself on Cloudsmith:</p>
<!--* [kurrent-lts](https://cloudsmith.io/~eventstore/repos/kurrent-lts/setup/#formats-rpm).-->
<ul>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-latest/setup/#formats-rpm" target="_blank" rel="noopener noreferrer">kurrent-latest</a>.</li>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-preview/setup/#formats-rpm" target="_blank" rel="noopener noreferrer">kurrent-preview</a>.</li>
</ul>
<h4 id="install-with-yum" tabindex="-1"><a class="header-anchor" href="#install-with-yum"><span>Install with yum</span></a></h4>
<p>Install the package:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">yum</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> install</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> kurrentdb-25.0.0-1.x86_64</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><h4 id="uninstall-with-yum" tabindex="-1"><a class="header-anchor" href="#uninstall-with-yum"><span>Uninstall with yum</span></a></h4>
<p>You can uninstall the package with:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">yum</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> remove</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> kurrentdb</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><h3 id="running-the-kurrentdb-service" tabindex="-1"><a class="header-anchor" href="#running-the-kurrentdb-service"><span>Running the kurrentdb service</span></a></h3>
<p>Once installed, the server is registered as a service. Therefore, you can start KurrentDB with:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">systemctl</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> start</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> kurrentdb</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>When you install the KurrentDB package, the service doesn't start by default. This allows you to change the configuration located at <code v-pre>/etc/kurrentdb/kurrentdb.conf</code> and to prevent creating database and index files in the default location.</p>
<div class="hint-container warning">
<p class="hint-container-title">Warning</p>
<p>We recommend that when using Linux you set the 'open file limit' to a high number. The precise value depends on your use case, but at least between <code v-pre>30,000</code> and <code v-pre>60,000</code>.</p>
</div>
<h2 id="windows" tabindex="-1"><a class="header-anchor" href="#windows"><span>Windows</span></a></h2>
<h3 id="nuget" tabindex="-1"><a class="header-anchor" href="#nuget"><span>NuGet</span></a></h3>
<p>KurrentDB has NuGet packages available on <a href="https://community.chocolatey.org/packages/kurrentdb" target="_blank" rel="noopener noreferrer">Chocolatey</a>.</p>
<h4 id="install-with-chocolatey" tabindex="-1"><a class="header-anchor" href="#install-with-chocolatey"><span>Install with Chocolatey</span></a></h4>
<p>You can install KurrentDB through Chocolatey:</p>
<div class="language-powershell line-numbers-mode" data-highlighter="shiki" data-ext="powershell" data-title="powershell" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">choco install kurrentdb </span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">--</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">version</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">=</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">25.0</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">.</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">0</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>KurrentDB can then be run with <code v-pre>KurrentDB.exe</code>:</p>
<div class="language-powershell line-numbers-mode" data-highlighter="shiki" data-ext="powershell" data-title="powershell" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2">KurrentDB.exe</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2"> --</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">config {your config file}</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><h4 id="uninstall-with-chocolatey" tabindex="-1"><a class="header-anchor" href="#uninstall-with-chocolatey"><span>Uninstall with Chocolatey</span></a></h4>
<p>You can uninstall KurrentDB through Chocolatey with:</p>
<div class="language-powershell line-numbers-mode" data-highlighter="shiki" data-ext="powershell" data-title="powershell" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">choco uninstall kurrentdb</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><h4 id="running-as-a-service" tabindex="-1"><a class="header-anchor" href="#running-as-a-service"><span>Running as a service</span></a></h4>
<p>KurrentDB can be run as a windows service. The following commands are for example. Consult the <a href="https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/sc-config" target="_blank" rel="noopener noreferrer">sc.exe documentation</a> for details.</p>
<p>Create the service:</p>
<div class="language-powershell line-numbers-mode" data-highlighter="shiki" data-ext="powershell" data-title="powershell" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2">sc.exe</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> create </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"KurrentDB"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> `</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  start</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">=</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> delayed</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">-</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">auto     `</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  binpath</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">=</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> "C:\ProgramData\chocolatey\lib\kurrentdb\kurrentdb-25.1.0-windows.x64\KurrentDB.exe --config=c:\path\to\kurrentdb-config.yaml"</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><p>Configure the restart policy:</p>
<div class="language-powershell line-numbers-mode" data-highlighter="shiki" data-ext="powershell" data-title="powershell" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2">sc.exe</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> failure </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"KurrentDB"</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> `</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  reset</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">=</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> 0</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">                 `</span></span>
<span class="line"><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">  actions</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">=</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> restart</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">5000</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">restart</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">5000</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF">restart</span><span style="--shiki-light:#383A42;--shiki-dark:#56B6C2">/</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66">5000</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><p>Start the service:</p>
<div class="language-powershell line-numbers-mode" data-highlighter="shiki" data-ext="powershell" data-title="powershell" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2">sc.exe</span><span style="--shiki-light:#383A42;--shiki-dark:#ABB2BF"> start </span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">"KurrentDB"</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>Logs will still be written to the usual log file location. By default on windows this is the <code v-pre>logs</code> directory within the directory that contains <code v-pre>KurrentDB.exe</code>.</p>
<h2 id="docker" tabindex="-1"><a class="header-anchor" href="#docker"><span>Docker</span></a></h2>
<p>You can run KurrentDB in a Docker container as a single node, using insecure mode. It is useful in most
cases to try out the product and for local development purposes.</p>
<p>It's also possible to run a three-node cluster with or without SSL using Docker Compose. Such a setup is
closer to what you'd run in production.</p>
<p>KurrentDB Docker images are hosted in the following registries:</p>
<!--* [kurrent-lts](https://cloudsmith.io/~eventstore/repos/kurrent-lts/packages/?q=format%3Adocker+name%3Akurrentdb) containing only production-ready [LTS](../release-schedule/#long-term-support-releases) containers.-->
<ul>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-latest/packages/?q=format%3Adocker+name%3Akurrentdb" target="_blank" rel="noopener noreferrer">kurrent-latest</a> containing production-ready LTS and <RouteLink to="/server/release-schedule/#short-term-support-releases">STS</RouteLink> containers.</li>
<li><a href="https://cloudsmith.io/~eventstore/repos/kurrent-preview/packages/?q=format%3Adocker+name%3Akurrentdb" target="_blank" rel="noopener noreferrer">kurrent-preview</a> containing non-production preview containers.</li>
</ul>
<h3 id="run-with-docker" tabindex="-1"><a class="header-anchor" href="#run-with-docker"><span>Run with Docker</span></a></h3>
<p>Pull the container with:</p>
<Tabs id="369" :data='[{"id":"kurrent-latest"}]'>
<template #title0="{ value, isActive }">kurrent-latest</template>
<template #tab0="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">docker</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> pull</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> docker.kurrent.io/kurrent-latest/kurrentdb:latest</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><!--@tab kurrent-lts
```bash
docker pull docker.kurrent.io/kurrent-lts/kurrentdb:latest
```-->
</template>
</Tabs>
<p>The following command will start the KurrentDB node using the default HTTP port, without security. You can then connect to it using one of the clients and the <code v-pre>kurrentdb://localhost:2113?tls=false</code> connection string. You can also access the Admin UI by opening http://localhost:2113 in your browser.</p>
<Tabs id="378" :data='[{"id":"kurrent-latest"}]'>
<template #title0="{ value, isActive }">kurrent-latest</template>
<template #tab0="{ value, isActive }">
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">docker</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> run</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> --name</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> kurrentdb-node</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -it</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> -p</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> 2113:2113</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#50A14F;--shiki-dark:#98C379">    docker.kurrent.io/kurrent-latest/kurrentdb:latest</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> --insecure</span><span style="--shiki-light:#986801;--shiki-dark:#D19A66"> --run-projections=All</span><span style="--shiki-light:#0184BC;--shiki-dark:#56B6C2"> \</span></span>
<span class="line"><span style="--shiki-light:#986801;--shiki-dark:#D19A66">    --enable-atom-pub-over-http</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div><div class="line-number"></div><div class="line-number"></div></div></div><!--@tab kurrent-lts
```bash
docker run --name kurrentdb-node -it -p 2113:2113 \
    docker.kurrent.io/kurrent-lts/kurrentdb:latest --insecure --run-projections=All \
    --enable-atom-pub-over-http
```-->
</template>
</Tabs>
<p>Then, you'd be able to connect to KurrentDB with gRPC clients. Also, the Stream Browser will work
in the Admin UI.</p>
<p>In order to sustainably keep the data, we also recommend mapping the database and index volumes.</p>
<h3 id="use-docker-compose" tabindex="-1"><a class="header-anchor" href="#use-docker-compose"><span>Use Docker Compose</span></a></h3>
<p>You can also run a single-node instance or a three-node secure cluster locally using Docker Compose.</p>
<h4 id="insecure-single-node" tabindex="-1"><a class="header-anchor" href="#insecure-single-node"><span>Insecure single node</span></a></h4>
<p>You can use Docker Compose to run KurrentDB in the same setup as the <code v-pre>docker run</code> command mentioned before.</p>
<p>Create a file <code v-pre>docker-compose.yaml</code> with the following content:</p>
<p>@<a href="@samples/docker-compose.yaml">code{curl}</a></p>
<p>Run the instance:</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">docker</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> compose</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> up</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>The command above would run KurrentDB as a single node without SSL. You also get AtomPub protocol enabled, so you can get the stream browser to work in the Admin UI.</p>
<div class="hint-container warning">
<p class="hint-container-title">Warning</p>
<p>The legacy TCP client protocol is disabled by default and is no longer be available from version 24.10.</p>
</div>
<h4 id="secure-cluster" tabindex="-1"><a class="header-anchor" href="#secure-cluster"><span>Secure cluster</span></a></h4>
<p>With Docker Compose, you can also run a three-node cluster with security enabled. This kind of setup is
something you'd expect to use in production.</p>
<p>Create file <code v-pre>docker-compose.yaml</code> with following content:</p>
<p>@<a href="@samples/docker-compose-cluster.yaml">code{curl}</a></p>
<p>Quite a few settings are shared between the nodes and we use the <code v-pre>env</code> file to avoid repeating those settings.
So, add the <code v-pre>vars.env</code> file to the same location:</p>
<p>@<a href="@samples/vars.env">code{curl}</a></p>
<p>Containers will use the shared volume using the local <code v-pre>./certs</code> directory for certificates. However, if you
let Docker create the directory on startup, the container won't be able to get write access to it.
Therefore, you should create the <code v-pre>certs</code> directory manually. You only need to do it once.</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">mkdir</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> certs</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>Now you are ready to start the cluster.</p>
<div class="language-bash line-numbers-mode" data-highlighter="shiki" data-ext="bash" data-title="bash" style="--shiki-light:#383A42;--shiki-dark:#abb2bf;--shiki-light-bg:#FAFAFA;--shiki-dark-bg:#282c34"><pre v-pre class="shiki shiki-themes one-light one-dark-pro vp-code"><code><span class="line"><span style="--shiki-light:#4078F2;--shiki-dark:#61AFEF">docker</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> compose</span><span style="--shiki-light:#50A14F;--shiki-dark:#98C379"> up</span></span></code></pre>
<div class="line-numbers" aria-hidden="true" style="counter-reset:line-number 0"><div class="line-number"></div></div></div><p>Watching the log messages, you will see that after some time, the elections process completes. Then you're able to connect to each
node using the Admin UI. Nodes should be accessible on the loopback address (<code v-pre>127.0.0.1</code> or <code v-pre>localhost</code>) over
HTTP, using ports specified below:</p>
<table>
<thead>
<tr>
<th style="text-align:left">Node</th>
<th style="text-align:left">HTTP port</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left">node1</td>
<td style="text-align:left">2111</td>
</tr>
<tr>
<td style="text-align:left">node2</td>
<td style="text-align:left">2112</td>
</tr>
<tr>
<td style="text-align:left">node3</td>
<td style="text-align:left">2113</td>
</tr>
</tbody>
</table>
<p>You have to tell your client to use secure connection.</p>
<table>
<thead>
<tr>
<th style="text-align:left">Protocol</th>
<th style="text-align:left">Connection string</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left">gRPC</td>
<td style="text-align:left"><code v-pre>kurrentdb://localhost:2111,localhost:2112,localhost:2113?tls=true&amp;tlsVerifyCert=false</code></td>
</tr>
</tbody>
</table>
<p>As you might've noticed, the connection string has a setting to disable the certificate validation (<code v-pre>tlsVerifyCert=false</code>). It would prevent the invalid certificate error since the cluster uses a private, auto-generated CA.</p>
<p>However, <strong>we do not recommend using this setting in production</strong>. Instead, you can either add the CA certificate to the trusted root CA store or instruct your application to use such a certificate. See the <RouteLink to="/server/security/protocol-security.html#certificate-installation-on-a-client-environment">security section</RouteLink> for detailed instructions.</p>
<h2 id="kubernetes" tabindex="-1"><a class="header-anchor" href="#kubernetes"><span>Kubernetes</span></a></h2>
<p>KurrentDB can be deployed and managed using the <RouteLink to="/server/kubernetes-operator/">Operator</RouteLink>.</p>
<h2 id="building-from-source" tabindex="-1"><a class="header-anchor" href="#building-from-source"><span>Building from source</span></a></h2>
<p>You can also build <a href="https://github.com/EventStore/EventStore?tab=readme-ov-file#building-kurrentdb" target="_blank" rel="noopener noreferrer">KurrentDB from source</a>. Before doing that, you need to install the .NET 8 SDK. KurrentDB packages have the .NET Runtime embedded, so you don't need to install anything except the KurrentDB package.</p>
<h2 id="compatibility-notes" tabindex="-1"><a class="header-anchor" href="#compatibility-notes"><span>Compatibility notes</span></a></h2>
<p>Depending on how your KurrentDB instance is configured, some features might not work. Below are some features that are unavailable due to the specified options.</p>
<table>
<thead>
<tr>
<th style="text-align:left">Feature</th>
<th style="text-align:left">Options impact</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left">Connection without SSL or TLS</td>
<td style="text-align:left">KurrentDB is secure by default. Your clients need to establish a secure connection, unless you use the <code v-pre>Insecure</code> option.</td>
</tr>
<tr>
<td style="text-align:left">Authentication and ACLs</td>
<td style="text-align:left">When using the <code v-pre>Insecure</code> option for the server, all security is disabled. The <code v-pre>Users</code> menu item is also disabled in the Admin UI.</td>
</tr>
<tr>
<td style="text-align:left">Projections</td>
<td style="text-align:left">Running projections is disabled by default and the <code v-pre>Projections</code> menu item is disabled in the Admin UI. You need to enable projections explicitly by using the <code v-pre>RunProjections</code> option.</td>
</tr>
<tr>
<td style="text-align:left">AtomPub protocol</td>
<td style="text-align:left">The AtomPub protocol is disabled by default. If you use this protocol, you have to explicitly enable it by using the <code v-pre>EnableAtomPubOverHttp</code> option.</td>
</tr>
<tr>
<td style="text-align:left">Stream browser</td>
<td style="text-align:left">The stream browser feature in Admin UI depends on the AtomPub protocol and is greyed out by default. You need to enable AtomPub (previous line) to make the stream browser work.</td>
</tr>
</tbody>
</table>
</div></template>


