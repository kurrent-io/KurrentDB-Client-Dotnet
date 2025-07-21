import comp from "C:/Code/KurrentDB-Client-Dotnet/docs/.vuepress/.temp/pages/api/release-notes.html.vue"
const data = JSON.parse("{\"path\":\"/api/release-notes.html\",\"title\":\"Release Notes\",\"lang\":\"en-US\",\"frontmatter\":{\"order\":10,\"head\":[[\"title\",{},\"Release Notes | .NET | Clients | Kurrent Docs\"]],\"gitInclude\":[]},\"headers\":[],\"readingTime\":{\"minutes\":0.15,\"words\":45},\"filePathRelative\":\"api/release-notes.md\"}")
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
