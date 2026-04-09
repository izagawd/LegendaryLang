// Wrapper implements Receiver but NOT Deref. Calling w.get() should fail —
// Receiver alone enables method dispatch but *w (bare deref) requires Deref.
// Without Deref, the auto-deref chain can still find methods but codegen
// needs the deref method. Let's test: method dispatch finds get() on Foo
// through Receiver, but it should still work since auto-deref only needs
// Receiver for method lookup.
// Actually — let's test that a type with NO Receiver at all can't dispatch.
struct Foo { val: i32 }
struct Opaque { data: Foo }

impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let o = make Opaque { data: make Foo { val: 42 } };
    o.get()
}
