export const redirects = JSON.parse("{}")

export const routes = Object.fromEntries([
  ["/api/getting-started.html", { loader: () => import(/* webpackChunkName: "api_getting-started.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/getting-started.html.js"), meta: {"t":"Getting started","O":1} }],
  ["/api/", { loader: () => import(/* webpackChunkName: "api_index.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/index.html.js"), meta: {"t":".NET"} }],
  ["/404.html", { loader: () => import(/* webpackChunkName: "404.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/404.html.js"), meta: {"t":""} }],
  ["/", { loader: () => import(/* webpackChunkName: "index.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/index.html.js"), meta: {"t":""} }],
]);

if (import.meta.webpackHot) {
  import.meta.webpackHot.accept()
  if (__VUE_HMR_RUNTIME__.updateRoutes) {
    __VUE_HMR_RUNTIME__.updateRoutes(routes)
  }
  if (__VUE_HMR_RUNTIME__.updateRedirects) {
    __VUE_HMR_RUNTIME__.updateRedirects(redirects)
  }
}

if (import.meta.hot) {
  import.meta.hot.accept(({ routes, redirects }) => {
    __VUE_HMR_RUNTIME__.updateRoutes(routes)
    __VUE_HMR_RUNTIME__.updateRedirects(redirects)
  })
}
