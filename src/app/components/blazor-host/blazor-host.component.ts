import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-blazor-host',
  templateUrl: './blazor-host.component.html',
  styleUrls: ['./blazor-host.component.css']
})
export class BlazorHostComponent implements OnInit, OnDestroy {
  iframeSrc = '/new'; // default if nothing passed
  private sub?: Subscription;

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    // react to route changes (e.g. user navigates between different blazor paths)
    this.sub = this.route.paramMap.subscribe(params => {
      const blazorPath = params.get('blazorPath') ?? '';
      // Build Blazor URL: /new/<blazorPath>?embedded=true
      // Make sure we don't get double slashes
      const trimmed = blazorPath.replace(/^\/+/, '');
      this.iframeSrc = `/new/${trimmed}?embedded=true`;
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
