export const SEARCH_INDEX = [
  {
    "title": "Getting started",
    "headers": [
      {
        "level": 2,
        "title": "Connecting to EventStoreDB",
        "slug": "connecting-to-eventstoredb",
        "link": "#connecting-to-eventstoredb",
        "children": [
          {
            "level": 3,
            "title": "Required packages",
            "slug": "required-packages",
            "link": "#required-packages",
            "children": []
          },
          {
            "level": 3,
            "title": "Connection string",
            "slug": "connection-string",
            "link": "#connection-string",
            "children": []
          },
          {
            "level": 3,
            "title": "Creating a client",
            "slug": "creating-a-client",
            "link": "#creating-a-client",
            "children": []
          }
        ]
      }
    ],
    "path": "/api/getting-started.html",
    "pathLocale": "/",
    "extraFields": []
  },
  {
    "title": ".NET",
    "headers": [],
    "path": "/api/",
    "pathLocale": "/",
    "extraFields": []
  },
  {
    "title": "",
    "headers": [],
    "path": "/404.html",
    "pathLocale": "/",
    "extraFields": []
  },
  {
    "title": "",
    "headers": [],
    "path": "/",
    "pathLocale": "/",
    "extraFields": []
  }
]

if (import.meta.webpackHot) {
  import.meta.webpackHot.accept()
  if (__VUE_HMR_RUNTIME__.updateSearchIndex) {
    __VUE_HMR_RUNTIME__.updateSearchIndex(searchIndex)
  }
}

if (import.meta.hot) {
  import.meta.hot.accept(({ searchIndex }) => {
    __VUE_HMR_RUNTIME__.updateSearchIndex(searchIndex)
  })
}
