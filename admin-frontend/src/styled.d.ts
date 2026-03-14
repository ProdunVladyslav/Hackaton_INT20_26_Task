import 'styled-components'
import { AppTheme } from '@shared/theme/theme'

declare module 'styled-components' {
  export interface DefaultTheme extends AppTheme {}
}
