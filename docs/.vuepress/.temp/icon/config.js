import { hasGlobalComponent } from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+helper@2.0.0-rc.74_typescript@5.5.3_vuepress@2.0.0-rc.19_@vuepress+bundler-vite@2.0_dtrpbp2lnh37xz2olmr2viykdm/node_modules/@vuepress/helper/lib/client/index.js";
import { useScriptTag } from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vueuse+core@12.5.0_typescript@5.5.3/node_modules/@vueuse/core/index.mjs";
import { h } from "vue";
import { VPIcon } from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+plugin-icon@2.0.0-rc.74_markdown-it@14.1.0_typescript@5.5.3_vuepress@2.0.0-rc.19_@v_brz3rqtsdqmzfyx7r2hdlsddka/node_modules/@vuepress/plugin-icon/lib/client/index.js"

export default {
  enhance: ({ app }) => {
    if(!hasGlobalComponent("VPIcon")) {
      app.component(
        "VPIcon",
        (props) =>
          h(VPIcon, {
            type: "iconify",
            prefix: "",
            ...props,
          })
      )
    }
  },
  setup: () => {
    useScriptTag(`https://cdn.jsdelivr.net/npm/iconify-icon@2`);
  },
}
