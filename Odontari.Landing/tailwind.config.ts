import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
    "./components/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        navy: {
          deep: "#001D39",
          mid: "#0A4174",
          light: "#0d3560",
        },
        slate: "#49769F",
        teal: {
          DEFAULT: "#4E8EA2",
          light: "#6EA2B3",
          dark: "#3A7085",
        },
        sky: "#7BBDE8",
        pale: "#BDD8E9",
        pearl: "#F4F8FB",
      },
      fontFamily: {
        sans: ["DM Sans", "system-ui", "sans-serif"],
        display: ["Sora", "system-ui", "sans-serif"],
      },
      animation: {
        "fade-up": "fadeUp 0.6s ease forwards",
        "float": "float 6s ease-in-out infinite",
        "float-slow": "float 9s ease-in-out infinite",
        "pulse-teal": "pulseTeal 3s ease-in-out infinite",
      },
      keyframes: {
        fadeUp: {
          "0%": { opacity: "0", transform: "translateY(24px)" },
          "100%": { opacity: "1", transform: "translateY(0)" },
        },
        float: {
          "0%, 100%": { transform: "translateY(0px)" },
          "50%": { transform: "translateY(-10px)" },
        },
        pulseTeal: {
          "0%, 100%": { boxShadow: "0 0 0 0 rgba(78,142,162,0)" },
          "50%": { boxShadow: "0 0 0 12px rgba(78,142,162,0.08)" },
        },
      },
    },
  },
  plugins: [],
};

export default config;
