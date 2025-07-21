import { Layout, NotFound, injectDarkmode, setupDarkmode, setupSidebarItems, scrollPromise } from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/vuepress-theme-hope@2.0.0-rc.71_@vuepress+plugin-search@2.0.0-rc.74_typescript@5.5.3_vuepress_ukau6s3ya2gvk3h4cbnbvhz2la/node_modules/vuepress-theme-hope/lib/bundle/export.js";

import { defineCatalogInfoGetter } from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+plugin-catalog@2.0.0-rc.74_typescript@5.5.3_vuepress@2.0.0-rc.19_@vuepress+bundler-_qci3qtf6g6ntij3hadrhnfgiqu/node_modules/@vuepress/plugin-catalog/lib/client/index.js"
import { h } from "vue"
import { resolveComponent } from "vue"

import "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+helper@2.0.0-rc.74_typescript@5.5.3_vuepress@2.0.0-rc.19_@vuepress+bundler-vite@2.0_dtrpbp2lnh37xz2olmr2viykdm/node_modules/@vuepress/helper/lib/client/styles/colors.css";
import "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+helper@2.0.0-rc.74_typescript@5.5.3_vuepress@2.0.0-rc.19_@vuepress+bundler-vite@2.0_dtrpbp2lnh37xz2olmr2viykdm/node_modules/@vuepress/helper/lib/client/styles/normalize.css";
import "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+helper@2.0.0-rc.74_typescript@5.5.3_vuepress@2.0.0-rc.19_@vuepress+bundler-vite@2.0_dtrpbp2lnh37xz2olmr2viykdm/node_modules/@vuepress/helper/lib/client/styles/sr-only.css";
import "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/vuepress-theme-hope@2.0.0-rc.71_@vuepress+plugin-search@2.0.0-rc.74_typescript@5.5.3_vuepress_ukau6s3ya2gvk3h4cbnbvhz2la/node_modules/vuepress-theme-hope/lib/bundle/styles/all.scss";

defineCatalogInfoGetter((meta) => {
  const title = meta.t;
  const shouldIndex = meta.I !== false;
  const icon = meta.i;

  return shouldIndex ? {
    title,
    content: icon ? () =>[h(resolveComponent("VPIcon"), { icon }), title] : null,
    order: meta.O,
    index: meta.I,
  } : null;
});

export default {
  enhance: ({ app, router }) => {
    const { scrollBehavior } = router.options;

    router.options.scrollBehavior = async (...args) => {
      await scrollPromise.wait();

      return scrollBehavior(...args);
    };

    // inject global properties
    injectDarkmode(app);


  },
  setup: () => {
    setupDarkmode();
    setupSidebarItems();

  },
  layouts: {
    Layout,
    NotFound,

  }
};
