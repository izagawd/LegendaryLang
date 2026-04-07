trait Foo {
    let Bruh :! type;
    fn dude() -> Self.Bruh;
}

trait Bar: Foo {}

impl Foo for i32 {
    let Bruh :! type = Self;
    fn dude() -> (Self as Foo).Bruh {
        4
    }
}

impl Bar for i32 {}

fn main() -> i32 {
    i32.dude()
}
