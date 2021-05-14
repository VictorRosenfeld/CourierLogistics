
namespace SQLCLR.Orders
{
    /// <summary>
    /// ��������� ������
    /// </summary>
    internal enum OrderStatus
    {
        /// <summary>
        /// �������������� ���������
        /// </summary>
        None = 0,

        /// <summary>
        /// �������� � �������
        /// </summary>
        Receipted = 1,

        /// <summary>
        /// ������ � ����� � ��������
        /// </summary>
        Assembled = 2,

        /// <summary>
        /// �������
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// ��������
        /// </summary>
        Completed = 4,
    }
}
