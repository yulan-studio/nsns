document.addEventListener("DOMContentLoaded", () => {
    
    //slide show section
    
    let currentSlide = 0;
    const slides = document.querySelectorAll('.slide');
    const totalSlides = slides.length;
  
    function showNextSlide() {
      slides[currentSlide].classList.remove('active');
      currentSlide = (currentSlide + 1) % totalSlides;
      slides[currentSlide].classList.add('active');
    }
  
    setInterval(showNextSlide, 5000); // every 5 seconds

   //accordion list
   const accordionHeaders = document.querySelectorAll('.accordion-header');

   accordionHeaders.forEach(header => {
     header.addEventListener('click', () => {
       const expanded = header.getAttribute('aria-expanded') === 'true';
       const content = header.nextElementSibling;
       const accordionItem = header.parentElement;

       // Close all other accordion items (optional: remove this loop for independent toggles)
       accordionHeaders.forEach(otherHeader => {
        const otherAccordionItem = otherHeader.parentElement;
         if (otherHeader !== header) {
           otherHeader.setAttribute('aria-expanded', 'false');
           otherHeader.nextElementSibling.hidden = true;
           otherHeader.querySelector('.arrow').textContent = '▼';
           otherAccordionItem.classList.remove('active');
         }
       });

       // Toggle current accordion item
       header.setAttribute('aria-expanded', String(!expanded));
       content.hidden = expanded;
       if(!expanded)
            accordionItem.classList.add('active');
       else
            accordionItem.classList.remove('active');
       header.querySelector('.arrow').textContent = expanded ? '▼' : '▲';
     });
   });

});


