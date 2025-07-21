export const redirects = JSON.parse("{}")

export const routes = Object.fromEntries([
  ["/api/appending-events.html", { loader: () => import(/* webpackChunkName: "api_appending-events.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/appending-events.html.js"), meta: {"t":"Appending events","O":2} }],
  ["/api/authentication.html", { loader: () => import(/* webpackChunkName: "api_authentication.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/authentication.html.js"), meta: {"t":"Authentication","O":7} }],
  ["/api/delete-stream.html", { loader: () => import(/* webpackChunkName: "api_delete-stream.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/delete-stream.html.js"), meta: {"t":"Deleting Events","O":9} }],
  ["/api/getting-started.html", { loader: () => import(/* webpackChunkName: "api_getting-started.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/getting-started.html.js"), meta: {"t":"Getting started","O":1} }],
  ["/api/observability.html", { loader: () => import(/* webpackChunkName: "api_observability.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/observability.html.js"), meta: {"t":"Observability","O":8} }],
  ["/api/persistent-subscriptions.html", { loader: () => import(/* webpackChunkName: "api_persistent-subscriptions.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/persistent-subscriptions.html.js"), meta: {"t":"Persistent Subscriptions","O":5} }],
  ["/api/projections.html", { loader: () => import(/* webpackChunkName: "api_projections.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/projections.html.js"), meta: {"t":"Projections","O":6} }],
  ["/api/reading-events.html", { loader: () => import(/* webpackChunkName: "api_reading-events.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/reading-events.html.js"), meta: {"t":"Reading Events","O":3} }],
  ["/api/", { loader: () => import(/* webpackChunkName: "api_index.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/index.html.js"), meta: {"t":".NET"} }],
  ["/api/subscriptions.html", { loader: () => import(/* webpackChunkName: "api_subscriptions.html" */"C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/subscriptions.html.js"), meta: {"t":"Catch-up Subscriptions","O":4} }],
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
