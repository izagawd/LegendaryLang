use Std.Ops.PartialEq;

struct Wrapper { val: i32 }

impl PartialEq(Wrapper) for Wrapper {
    fn Eq(lhs: &Wrapper, rhs: &Wrapper) -> bool {
        lhs.val == rhs.val
    }
}

fn main() -> i32 {
    let a = make Wrapper { val: 42 };
    let b = make Wrapper { val: 42 };
    let eq1 = a == b;
    let eq2 = a == b;
    if eq1 && eq2 { 1 } else { 0 }
}
