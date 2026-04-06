trait Drop {
    fn Drop(self: &uniq Self);
}

trait Add(Rhs:! type) {
    type Output;
    fn Add(lhs: Self, rhs: Rhs) -> Self.Output;
}

trait Sub(Rhs:! type) {
    type Output;
    fn Sub(lhs: Self, rhs: Rhs) -> Self.Output;
}

trait Mul(Rhs:! type) {
    type Output;
    fn Mul(lhs: Self, rhs: Rhs) -> Self.Output;
}

trait Div(Rhs:! type) {
    type Output;
    fn Div(lhs: Self, rhs: Rhs) -> Self.Output;
}

impl Add(i32) for i32 {
    type Output = i32;
    fn Add(lhs: i32, rhs: i32) -> i32 {
        lhs + rhs
    }
}

impl Sub(i32) for i32 {
    type Output = i32;
    fn Sub(lhs: i32, rhs: i32) -> i32 {
        lhs - rhs
    }
}

impl Mul(i32) for i32 {
    type Output = i32;
    fn Mul(lhs: i32, rhs: i32) -> i32 {
        lhs * rhs
    }
}

impl Div(i32) for i32 {
    type Output = i32;
    fn Div(lhs: i32, rhs: i32) -> i32 {
        lhs / rhs
    }
}

trait PartialEq(Rhs:! type) {
    fn Eq(lhs: Self, rhs: Rhs) -> bool;
    fn Ne(lhs: Self, rhs: Rhs) -> bool {
        !(lhs == rhs)
    }
}

trait Eq: PartialEq(Self) {}

trait PartialOrd(Rhs:! type): PartialEq(Rhs) {
    fn Lt(lhs: Self, rhs: Rhs) -> bool;
    fn Gt(lhs: Self, rhs: Rhs) -> bool;
}

trait Ord: PartialOrd(Self) + Eq {}

impl PartialEq(i32) for i32 {
    fn Eq(lhs: i32, rhs: i32) -> bool {
        lhs == rhs
    }
}

impl Eq for i32 {}

impl PartialOrd(i32) for i32 {
    fn Lt(lhs: i32, rhs: i32) -> bool {
        lhs < rhs
    }
    fn Gt(lhs: i32, rhs: i32) -> bool {
        lhs > rhs
    }
}

impl Ord for i32 {}

impl PartialEq(bool) for bool {
    fn Eq(lhs: bool, rhs: bool) -> bool {
        lhs == rhs
    }
}

impl Eq for bool {}

impl PartialEq(u8) for u8 {
    fn Eq(lhs: u8, rhs: u8) -> bool {
        lhs == rhs
    }
}

impl Eq for u8 {}

impl PartialOrd(u8) for u8 {
    fn Lt(lhs: u8, rhs: u8) -> bool {
        lhs < rhs
    }
    fn Gt(lhs: u8, rhs: u8) -> bool {
        lhs > rhs
    }
}

impl Ord for u8 {}

impl PartialEq(usize) for usize {
    fn Eq(lhs: usize, rhs: usize) -> bool {
        lhs == rhs
    }
}

impl Eq for usize {}

impl PartialOrd(usize) for usize {
    fn Lt(lhs: usize, rhs: usize) -> bool {
        lhs < rhs
    }
    fn Gt(lhs: usize, rhs: usize) -> bool {
        lhs > rhs
    }
}

impl Ord for usize {}
