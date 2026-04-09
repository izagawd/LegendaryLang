use Std.Ops.Drop;

struct Foo['a] {
    dd: &'a mut i32
}

impl['a] Drop for Foo['a] {
    fn Drop(self: &uniq Self) {
        *self.dd = *self.dd + 1;
    }
}

fn main() -> i32 {
    let foo = 5;
    {
        let bruh = make Foo { dd: &mut foo };
    }
    foo
}
