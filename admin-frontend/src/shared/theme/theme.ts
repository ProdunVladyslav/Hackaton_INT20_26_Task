export const lightTheme = {
  mode: 'light' as const,
  colors: {
    // Backgrounds
    bg: '#FAFAFA',
    bgSurface: '#FFFFFF',
    bgElevated: '#F4F4F5',
    bgOverlay: 'rgba(0,0,0,0.4)',

    // Text
    textPrimary: '#18181B',
    textSecondary: '#71717A',
    textTertiary: '#A1A1AA',
    textInverse: '#FFFFFF',

    // Brand / Accent
    accent: '#6366F1',
    accentHover: '#4F46E5',
    accentLight: '#EEF2FF',
    accentText: '#4338CA',

    // Status
    success: '#22C55E',
    successLight: '#DCFCE7',
    warning: '#F59E0B',
    warningLight: '#FEF3C7',
    error: '#EF4444',
    errorLight: '#FEE2E2',
    info: '#3B82F6',
    infoLight: '#DBEAFE',

    // Borders
    border: '#E4E4E7',
    borderFocus: '#6366F1',
    borderHover: '#D4D4D8',

    // Shadows
    shadow: 'rgba(0,0,0,0.06)',
    shadowMd: 'rgba(0,0,0,0.10)',

    // Node colors for DAG
    nodeQuestion: '#6366F1',
    nodeInfo: '#10B981',
    nodeOffer: '#F59E0B',
  },
  shadows: {
    sm: '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
    md: '0 4px 12px rgba(0,0,0,0.08), 0 2px 6px rgba(0,0,0,0.04)',
    lg: '0 10px 28px rgba(0,0,0,0.10), 0 4px 10px rgba(0,0,0,0.06)',
    xl: '0 20px 48px rgba(0,0,0,0.12), 0 8px 20px rgba(0,0,0,0.08)',
  },
  spacing: {
    xs: '4px',
    sm: '8px',
    md: '16px',
    lg: '24px',
    xl: '32px',
    xxl: '48px',
    xxxl: '64px',
  },
  radii: {
    sm: '6px',
    md: '10px',
    lg: '14px',
    xl: '20px',
    full: '9999px',
  },
  typography: {
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    sizes: {
      xs: '11px',
      sm: '13px',
      md: '15px',
      lg: '18px',
      xl: '22px',
      xxl: '28px',
      xxxl: '36px',
    },
    weights: {
      light: 300,
      regular: 400,
      medium: 500,
      semibold: 600,
      bold: 700,
    },
    lineHeights: {
      tight: 1.25,
      normal: 1.5,
      relaxed: 1.75,
    },
  },
  transitions: {
    fast: '0.15s ease',
    normal: '0.3s ease',
    slow: '0.5s ease',
  },
  breakpoints: {
    mobile: '768px',
    tablet: '1024px',
    laptop: '1440px',
  },
}

export const darkTheme: typeof lightTheme = {
  ...lightTheme,
  mode: 'dark' as const,
  colors: {
    bg: '#09090B',
    bgSurface: '#18181B',
    bgElevated: '#27272A',
    bgOverlay: 'rgba(0,0,0,0.6)',

    textPrimary: '#FAFAFA',
    textSecondary: '#A1A1AA',
    textTertiary: '#71717A',
    textInverse: '#18181B',

    accent: '#818CF8',
    accentHover: '#6366F1',
    accentLight: '#1E1B4B',
    accentText: '#A5B4FC',

    success: '#4ADE80',
    successLight: '#052E16',
    warning: '#FCD34D',
    warningLight: '#292524',
    error: '#F87171',
    errorLight: '#2D0F0F',
    info: '#60A5FA',
    infoLight: '#1E3A5F',

    border: '#3F3F46',
    borderFocus: '#818CF8',
    borderHover: '#52525B',

    shadow: 'rgba(0,0,0,0.3)',
    shadowMd: 'rgba(0,0,0,0.4)',

    nodeQuestion: '#818CF8',
    nodeInfo: '#34D399',
    nodeOffer: '#FCD34D',
  },
  shadows: {
    sm: '0 1px 3px rgba(0,0,0,0.3)',
    md: '0 4px 12px rgba(0,0,0,0.4)',
    lg: '0 10px 28px rgba(0,0,0,0.5)',
    xl: '0 20px 48px rgba(0,0,0,0.6)',
  },
}

export type AppTheme = typeof lightTheme
