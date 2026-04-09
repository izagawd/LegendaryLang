// r is &Box<Foo>. r.get_val() — auto-deref: &Box → Box → Foo. get_val takes &Self.
struct Foo { val: i32 }
impl Foo { fn get_val(self: &Self) -> i32 { self.val } }
fn read_box(r: &Box(Foo)) -> i32 { r.get_val() }
fn main() -> i32 {
    let b = Box.New(make Foo { val: 42 });
    read_box(&b)
}
