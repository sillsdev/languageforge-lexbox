const path = require('path');
const tailwindConfig = path.join(__dirname, 'tailwind.config.ts');

module.exports = {
  plugins: {
    'tailwindcss/nesting': {},
    tailwindcss: {
      config: tailwindConfig,
    },
    autoprefixer: {},
  },
};
