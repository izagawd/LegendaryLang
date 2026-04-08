trait Foo {
    fn Bro(self: &Self) -> i32;
}

struct Bar { val: i32 }
impl Foo for Bar {
    fn Bro(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let b = make Bar { val: 10 };
    b.Bro()
}
