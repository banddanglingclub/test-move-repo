import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-blazor-host',
  templateUrl: './blazor-host.component.html',
  styleUrls: ['./blazor-host.component.css']
})
export class BlazorHostComponent implements OnInit, OnDestroy {
  iframeSrc!: SafeResourceUrl;
  private sub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.sub = this.route.paramMap.subscribe(params => {
      const blazorPath = params.get('blazorPath') ?? '';
      const trimmed = blazorPath.replace(/^\/+/, '');

      const url = `/new/${trimmed}?embedded=true`;
      // Tell Angular “this URL is safe for an iframe”
      this.iframeSrc = this.sanitizer.bypassSecurityTrustResourceUrl(url);
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
