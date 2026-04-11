use Std.Deref.Deref;

struct Foo {}
impl Copy for Foo {}
impl Foo {
    fn faah(self: &Self) -> i32 { 42 }
}

fn bro(T:! Sized +Deref(Target = Foo), dd: T) -> i32 {
    dd.faah()
}

fn main() -> i32 {
    let bruh = make Foo {};
    bro(&Foo, &bruh)
}
