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
    env: {
      ...loadEnv('test', process.cwd(), ''),
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
        '/api': env.services__api__https__0 || env.services__api__http__0,
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
