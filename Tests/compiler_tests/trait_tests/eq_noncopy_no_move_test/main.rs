use Std.Ops.PartialEq;

struct Heavy { val: i32 }

impl PartialEq(Heavy) for Heavy {
    fn Eq(lhs: Heavy, rhs: Heavy) -> bool {
        lhs.val == rhs.val
    }
}

fn main() -> i32 {
    let a = make Heavy { val: 10 };
    let b = make Heavy { val: 10 };
    let eq1 = a == b;
    let eq2 = a == b;
    let eq3 = a != b;
    let result = 0;
    if eq1 { result = result + 1; };
    if eq2 { result = result + 10; };
    if eq3 { result = result + 100; };
    result
}
