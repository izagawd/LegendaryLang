// b consumes GcMut<Foo> via into_bar(self:Self). Result is Bar. Then bar.get().
// GcMut is non-Copy so b is moved into the chain.
struct Foo { val: i32 }
struct Bar { inner: i32 }
impl Foo { fn into_bar(self: Self) -> Bar { make Bar { inner: self.val } } }
impl Bar { fn get(self: &Self) -> i32 { self.inner } }
fn main() -> i32 {
    let f = make Foo { val: 42 };
    f.into_bar().get()
}
