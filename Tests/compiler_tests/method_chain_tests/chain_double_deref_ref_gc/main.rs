// r is &GcMut<Foo>. r.get_val() — auto-deref: &GcMut → GcMut → Foo. get_val takes &Self.
struct Foo { val: i32 }
impl Foo { fn get_val(self: &Self) -> i32 { self.val } }
fn read_box(r: &GcMut(Foo)) -> i32 { r.get_val() }
fn main() -> i32 {
    let b = GcMut.New(make Foo { val: 42 });
    read_box(&b)
}
