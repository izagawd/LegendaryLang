// bruh.dd where bruh: &Foo and dd: GcMut(i32) is non-Copy.
// Cannot move a non-Copy field out through a reference.

struct Foo { dd: GcMut(i32) }

fn main() -> i32 {
    let made = make Foo { dd: GcMut.New(4) };
    let bruh = &made;
    let idk = bruh.dd;
    6
}
