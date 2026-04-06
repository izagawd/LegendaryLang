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
