import { defineConfig, loadEnv, mergeConfig } from 'vite'
import { defineConfig as defineTestConfig } from 'vitest/config'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from '@tailwindcss/vite'

// https://vitejs.dev/config/
const testConfig = defineTestConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/testing/test-setup.ts'],
    environmentOptions: {
      jsdom: {
        resources: 'usable',
      },
    },
  },
})

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  const config = defineConfig({
    plugins: [react(), tailwindcss()],
    server: {
      port: parseInt(env.PORT),
      proxy: {
        '/api': {
          target:
            process.env.services__api__https__0 ??
            process.env.services__api__http__0,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, ''),
          secure: false,
        },
      },
    },
    resolve: {
      alias: {
        src: '/src',
        components: '/src/components',
      },
    },
  })
  return mergeConfig(config, testConfig)
})
