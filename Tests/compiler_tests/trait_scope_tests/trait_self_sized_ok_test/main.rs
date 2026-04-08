trait Foo: Sized {
    fn Bro(input: Self) -> i32;
}

struct Bar { val: i32 }
impl Foo for Bar {
    fn Bro(input: Self) -> i32 { input.val }
}

fn main() -> i32 {
    let b = make Bar { val: 42 };
    (Bar as Foo).Bro(b)
}
