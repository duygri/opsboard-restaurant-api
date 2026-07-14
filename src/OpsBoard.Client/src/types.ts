import type { ReactNode } from 'react'

export type UserRole = 'Admin' | 'Staff'
export type OrderStatus = 'New' | 'Preparing' | 'Ready' | 'Served' | 'Paid' | 'Cancelled'
export type TableStatus = 'Available' | 'Occupied' | 'Reserved'
export type ViewKey = 'floor' | 'orders' | 'report' | 'audit'

export type CurrentUser = {
  id: string
  fullName: string
  email: string
  role: UserRole
}

export type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  user: CurrentUser
}

export type TableResponse = {
  id: string
  name: string
  status: TableStatus
}

export type MenuCategoryResponse = {
  id: string
  name: string
  displayOrder: number
}

export type MenuItemResponse = {
  id: string
  categoryId: string
  name: string
  description: string
  price: number
  isAvailable: boolean
  isLowStock: boolean
}

export type OrderItemResponse = {
  id: string
  menuItemId: string
  itemNameSnapshot: string
  unitPriceSnapshot: number
  quantity: number
  lineTotal: number
}

export type OrderDetailResponse = {
  id: string
  tableId: string
  tableName: string
  status: OrderStatus
  subtotal: number
  total: number
  createdAtUtc: string
  paidAtUtc: string | null
  items: OrderItemResponse[]
}

export type DailyReportResponse = {
  date: string
  timeZone: string
  revenue: number
  paidOrderCount: number
  statusCounts: Array<{ status: OrderStatus; count: number }>
}

export type AuditLogResponse = {
  id: string
  actorName: string
  action: string
  entityType: string
  entityId: string
  createdAtUtc: string
}

export type Session = {
  token: string
  user: CurrentUser
}

export type CartLine = {
  item: MenuItemResponse
  quantity: number
  lineTotal: number
}

export type IconNode = ReactNode
