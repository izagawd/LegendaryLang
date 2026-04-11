// r is &Gc<Foo>. r.get_val() — auto-deref: &Gc → Gc → Foo. get_val takes &Self.
struct Foo { val: i32 }
impl Foo { fn get_val(self: &Self) -> i32 { self.val } }
fn read_box(r: &Gc(Foo)) -> i32 { r.get_val() }
fn main() -> i32 {
    let b = Gc.New(make Foo { val: 42 });
    read_box(&b)
}
