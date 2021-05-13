
namespace SQLCLR.Shop
{
    using System;

    /// <summary>
    /// �������
    /// </summary>
    internal class Shop
    {
        /// <summary>
        /// Id ��������
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// ������ ������
        /// </summary>
        public TimeSpan WorkStart { get; set; }

        /// <summary>
        /// ����� ������
        /// </summary>
        public TimeSpan WorkEnd { get; set; }

        /// <summary>
        /// ������ ��������
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// ������� ��������
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// ��������������� ����������� ������ Shop
        /// </summary>
        /// <param name="id">Id ��������</param>
        public Shop(int id)
        {
            Id = id;
        }
    }
}
