import { createGlobalStyle } from 'styled-components'

export const GlobalStyles = createGlobalStyle`
  *, *::before, *::after {
    box-sizing: border-box;
    margin: 0;
    padding: 0;
  }

  html, body {
    height: 100%;
    overflow: hidden;
  }

  html {
    font-size: 16px;
    -webkit-text-size-adjust: 100%;
  }

  body {
    font-family: ${({ theme }) => theme.typography.fontFamily};
    font-size: ${({ theme }) => theme.typography.sizes.md};
    font-weight: ${({ theme }) => theme.typography.weights.regular};
    line-height: ${({ theme }) => theme.typography.lineHeights.normal};
    background-color: ${({ theme }) => theme.colors.bg};
    color: ${({ theme }) => theme.colors.textPrimary};
    transition: background-color ${({ theme }) => theme.transitions.normal},
                color ${({ theme }) => theme.transitions.normal};
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  }

  #root {
    height: 100%;
    display: flex;
    flex-direction: column;
  }

  a {
    color: inherit;
    text-decoration: none;
  }

  button {
    cursor: pointer;
    border: none;
    background: none;
    font-family: inherit;
  }

  input, textarea, select {
    font-family: inherit;
  }

  ul, ol {
    list-style: none;
  }

  img {
    max-width: 100%;
    display: block;
  }

  /* Scrollbar */
  ::-webkit-scrollbar {
    width: 6px;
    height: 6px;
  }
  ::-webkit-scrollbar-track {
    background: transparent;
  }
  ::-webkit-scrollbar-thumb {
    background: ${({ theme }) => theme.colors.border};
    border-radius: 3px;
  }
  ::-webkit-scrollbar-thumb:hover {
    background: ${({ theme }) => theme.colors.textTertiary};
  }

  /* ─── React Flow overrides ─────────────────────────────────────────── */
  .react-flow__attribution { display: none !important; }

  /* Ensure React Flow's wrapper divs fill their containers */
  .react-flow__renderer,
  .react-flow__viewport {
    width: 100%;
    height: 100%;
  }

  .react-flow__handle {
    width: 12px !important;
    height: 12px !important;
    border-radius: 50% !important;
    cursor: crosshair !important;
    opacity: 1 !important;
    transition: transform 0.15s ease !important;
  }
  .react-flow__handle:hover {
    transform: scale(1.4) !important;
  }
  .react-flow__handle-connecting {
    background: ${({ theme }) => theme.colors.accent} !important;
  }
  .react-flow__handle-valid {
    background: ${({ theme }) => theme.colors.success} !important;
  }

  .react-flow__controls-button {
    background: ${({ theme }) => theme.colors.bgSurface} !important;
    border-bottom-color: ${({ theme }) => theme.colors.border} !important;
    color: ${({ theme }) => theme.colors.textSecondary} !important;
    fill: ${({ theme }) => theme.colors.textSecondary} !important;
  }
  .react-flow__controls-button:hover {
    background: ${({ theme }) => theme.colors.bgElevated} !important;
  }

  .react-flow__node {
    border-radius: 12px !important;
  }
  .react-flow__node.selected > * {
    outline: none !important;
  }

  .react-flow__connection-line {
    stroke: ${({ theme }) => theme.colors.accent} !important;
    stroke-width: 2 !important;
    stroke-dasharray: 5 3 !important;
  }
  .react-flow__connection-path {
    stroke: ${({ theme }) => theme.colors.accent} !important;
  }
`
