use Std.Ops.Drop;

struct Foo['a] {
    kk: &'a mut i32
}

impl['a] Foo['a] {
    fn get_val(self: &Self) -> i32 {
        *self.kk
    }
}

impl['a] Drop for Foo['a] {
    fn Drop(self: &mut Self) {
        *self.kk = *self.kk + 1;
    }
}

fn main() -> i32 {
    let a = 0;
    let v = make Foo { kk: &mut a }.get_val();
    v + a
}
