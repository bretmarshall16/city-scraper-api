//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace city_scraper_api
{
    using System;
    using System.Collections.Generic;
    
    public partial class Player
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Player()
        {
            this.PlayerGames = new HashSet<PlayerGame>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public int TeamId { get; set; }
        public int Year { get; set; }
        public string Position { get; set; }
    
        public virtual Team Team { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlayerGame> PlayerGames { get; set; }
    }
}