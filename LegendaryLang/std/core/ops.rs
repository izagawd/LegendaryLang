trait Add<Rhs> {
    type Output;
    fn add(lhs: Self, rhs: Rhs) -> Output;
}

trait Sub<Rhs> {
    type Output;
    fn sub(lhs: Self, rhs: Rhs) -> Output;
}

trait Mul<Rhs> {
    type Output;
    fn mul(lhs: Self, rhs: Rhs) -> Output;
}

trait Div<Rhs> {
    type Output;
    fn div(lhs: Self, rhs: Rhs) -> Output;
}

impl Add<i32> for i32 {
    type Output = i32;
    fn add(lhs: i32, rhs: i32) -> i32 {
        lhs + rhs
    }
}

impl Sub<i32> for i32 {
    type Output = i32;
    fn sub(lhs: i32, rhs: i32) -> i32 {
        lhs - rhs
    }
}

impl Mul<i32> for i32 {
    type Output = i32;
    fn mul(lhs: i32, rhs: i32) -> i32 {
        lhs * rhs
    }
}

impl Div<i32> for i32 {
    type Output = i32;
    fn div(lhs: i32, rhs: i32) -> i32 {
        lhs / rhs
    }
}
