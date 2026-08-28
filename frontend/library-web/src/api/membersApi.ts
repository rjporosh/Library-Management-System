import { httpClient } from './httpClient'
import type { Member } from '../types/member'

export const membersApi = {
  async getById(id: string): Promise<Member> {
    const response = await httpClient.get<Member>(
      `/members/${id}`,
    )

    return response.data
  },
}