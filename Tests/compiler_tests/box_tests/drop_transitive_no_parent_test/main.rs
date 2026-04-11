use Std.Ops.Drop;
struct C['a] {
    r: &'a mut i32
}

impl['a] Drop for C['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 1;
    }
}

struct B['a] {
    c: C['a]
}

struct A['a] {
    b: B['a],
    val: i32
}

fn main() -> i32 {
    let counter = 0;
    {
        let a = make A {
            b: make B {
                c: make C { r: &mut counter }
            },
            val: 5
        };
    }
    counter
}
