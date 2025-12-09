import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { BlazorBridgeService } from 'src/app/services/blazor-bridge.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-blazor-host',
  templateUrl: './blazor-host.component.html',
  styleUrls: ['./blazor-host.component.css']
})
export class BlazorHostComponent implements OnInit, OnDestroy {

  @ViewChild('blazorFrame', { static: true })
  frameRef!: ElementRef<HTMLIFrameElement>;
  
  iframeSrc!: SafeResourceUrl;

  private readyListener?: (e: MessageEvent) => void;

  constructor(
    private sanitizer: DomSanitizer,
    private bridge: BlazorBridgeService,
    private router: Router
  ) {}

  ngOnInit(): void {

    // Which page did Angular ask for when navigating to /blazor?
    // Prefer router state when available, otherwise fall back to history.state
    const nav = this.router.getCurrentNavigation();
    const state = (nav && nav.extras && nav.extras.state) ? nav.extras.state : window.history.state;
    const page = state && state['blazorPage'] as string | undefined;

    let rawUrl: string;
    if (page) {
      // Start straight on /<page>?embedded=true
      rawUrl = `${environment.blazorBaseUrl}/${page}?embedded=true`;
    } else {
      // No specific page requested → default welcome
      rawUrl = `${environment.blazorBaseUrl}/?embedded=true`;
    }

    this.iframeSrc = this.sanitizer.bypassSecurityTrustResourceUrl(rawUrl);

    // Listen for "blazor-ready" so we know when we can send messages
    this.readyListener = (e: MessageEvent) => {
      console.log('[BlazorHost] message received', e);

      const allowedOrigins = [
        'https://localhost:7099',          // Blazor dev
        window.location.origin            // prod: same origin under /new
      ];

      if (!allowedOrigins.includes(e.origin)) {
        console.warn('[BlazorHost] ignoring message from unallowed origin', e.origin);
        return;
      }

      if (e.data?.type === 'blazor-ready') {
        this.bridge.setReady(true);
      }
    };
    window.addEventListener('message', this.readyListener);
  }

  ngAfterViewInit(): void {
    console.log('[BlazorHost] setting frame');
    this.bridge.setFrame(this.frameRef.nativeElement);
  }

  ngOnDestroy(): void {
    this.bridge.reset();
    if (this.readyListener) {
      window.removeEventListener('message', this.readyListener);
    }
  }
}
