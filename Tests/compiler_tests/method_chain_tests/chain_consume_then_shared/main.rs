// x.into_bar().get_val() — first consumes Foo (Self), returns Bar.
// Second takes &Self on the new Bar temporary.
struct Foo { val: i32 }
struct Bar { inner: i32 }
impl Foo { fn into_bar(self: Self) -> Bar { make Bar { inner: self.val } } }
impl Bar { fn get_val(self: &Self) -> i32 { self.inner } }
fn main() -> i32 {
    let f = make Foo { val: 42 };
    f.into_bar().get_val()
}
