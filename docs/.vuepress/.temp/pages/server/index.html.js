import comp from "C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/server/index.html.vue"
const data = JSON.parse("{\"path\":\"/server/\",\"title\":\"Server\",\"lang\":\"en-US\",\"frontmatter\":{\"title\":\"Server\",\"article\":false,\"feed\":false,\"sitemap\":false,\"gitInclude\":[]},\"headers\":[],\"readingTime\":{\"minutes\":0,\"words\":1},\"filePathRelative\":null}")
export { comp, data }

if (import.meta.webpackHot) {
  import.meta.webpackHot.accept()
  if (__VUE_HMR_RUNTIME__.updatePageData) {
    __VUE_HMR_RUNTIME__.updatePageData(data)
  }
}

if (import.meta.hot) {
  import.meta.hot.accept(({ data }) => {
    __VUE_HMR_RUNTIME__.updatePageData(data)
  })
}
