/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Views/**/*.cshtml", // đường dẫn đến Razor Views
    "./Views/**/*.html", // nếu bạn có HTML thuần
    "./Scripts/**/*.js", // nếu bạn có JavaScript
  ],
  theme: {
    extend: {},
  },
  plugins: [
    require("@tailwindcss/line-clamp"), // <== dòng cần thiết
  ],
};
