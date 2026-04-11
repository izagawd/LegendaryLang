use Std.Core.Ordering;

trait Drop {
    fn Drop(self: &mut Self);
}

trait Add(Rhs:! Sized): Sized {
    let Output :! Sized;
    fn Add(lhs: Self, rhs: Rhs) -> Self.Output;
}

trait Sub(Rhs:! Sized): Sized {
    let Output :! Sized;
    fn Sub(lhs: Self, rhs: Rhs) -> Self.Output;
}

trait Mul(Rhs:! Sized): Sized {
    let Output :! Sized;
    fn Mul(lhs: Self, rhs: Rhs) -> Self.Output;
}

trait Div(Rhs:! Sized): Sized {
    let Output :! Sized;
    fn Div(lhs: Self, rhs: Rhs) -> Self.Output;
}

impl Add(i32) for i32 {
    let Output :! Sized = i32;
    fn Add(lhs: i32, rhs: i32) -> i32 {
        lhs + rhs
    }
}

impl Sub(i32) for i32 {
    let Output :! Sized = i32;
    fn Sub(lhs: i32, rhs: i32) -> i32 {
        lhs - rhs
    }
}

impl Mul(i32) for i32 {
    let Output :! Sized = i32;
    fn Mul(lhs: i32, rhs: i32) -> i32 {
        lhs * rhs
    }
}

impl Div(i32) for i32 {
    let Output :! Sized = i32;
    fn Div(lhs: i32, rhs: i32) -> i32 {
        lhs / rhs
    }
}

trait PartialEq(Rhs:! Sized) {
    fn Eq(lhs: &Self, rhs: &Rhs) -> bool;
    fn Ne(lhs: &Self, rhs: &Rhs) -> bool {
        !(Self as PartialEq(Rhs)).Eq(lhs, rhs)
    }
}

trait Eq: PartialEq(Self) {}

trait PartialOrd(Rhs:! Sized): PartialEq(Rhs) {
    fn partial_cmp(lhs: &Self, rhs: &Rhs) -> Option(Ordering) {
        if (Self as PartialOrd(Rhs)).Lt(lhs, rhs) {
            Option.Some(Ordering.Less)
        } else {
            if (Self as PartialOrd(Rhs)).Gt(lhs, rhs) {
                Option.Some(Ordering.Greater)
            } else {
                Option.Some(Ordering.Equal)
            }
        }
    }
    fn Lt(lhs: &Self, rhs: &Rhs) -> bool;
    fn Gt(lhs: &Self, rhs: &Rhs) -> bool;
    fn Le(lhs: &Self, rhs: &Rhs) -> bool {
        !(Self as PartialOrd(Rhs)).Gt(lhs, rhs)
    }
    fn Ge(lhs: &Self, rhs: &Rhs) -> bool {
        !(Self as PartialOrd(Rhs)).Lt(lhs, rhs)
    }
}

trait Ord: PartialOrd(Self) + Eq {}

impl PartialEq(i32) for i32 {
    fn Eq(lhs: &i32, rhs: &i32) -> bool {
        *lhs == *rhs
    }
}

impl Eq for i32 {}

impl PartialOrd(i32) for i32 {
    fn Lt(lhs: &i32, rhs: &i32) -> bool {
        *lhs < *rhs
    }
    fn Gt(lhs: &i32, rhs: &i32) -> bool {
        *lhs > *rhs
    }
}

impl Ord for i32 {}

impl PartialEq(bool) for bool {
    fn Eq(lhs: &bool, rhs: &bool) -> bool {
        *lhs == *rhs
    }
}

impl Eq for bool {}

impl PartialEq(u8) for u8 {
    fn Eq(lhs: &u8, rhs: &u8) -> bool {
        *lhs == *rhs
    }
}

impl Eq for u8 {}

impl PartialOrd(u8) for u8 {
    fn Lt(lhs: &u8, rhs: &u8) -> bool {
        *lhs < *rhs
    }
    fn Gt(lhs: &u8, rhs: &u8) -> bool {
        *lhs > *rhs
    }
}

impl Ord for u8 {}

impl Add(u8) for u8 {
    let Output :! Sized = u8;
    fn Add(lhs: u8, rhs: u8) -> u8 {
        lhs + rhs
    }
}

impl Sub(u8) for u8 {
    let Output :! Sized = u8;
    fn Sub(lhs: u8, rhs: u8) -> u8 {
        lhs - rhs
    }
}

impl Mul(u8) for u8 {
    let Output :! Sized = u8;
    fn Mul(lhs: u8, rhs: u8) -> u8 {
        lhs * rhs
    }
}

impl Div(u8) for u8 {
    let Output :! Sized = u8;
    fn Div(lhs: u8, rhs: u8) -> u8 {
        lhs / rhs
    }
}

impl PartialEq(usize) for usize {
    fn Eq(lhs: &usize, rhs: &usize) -> bool {
        *lhs == *rhs
    }
}

impl Eq for usize {}

impl PartialOrd(usize) for usize {
    fn Lt(lhs: &usize, rhs: &usize) -> bool {
        *lhs < *rhs
    }
    fn Gt(lhs: &usize, rhs: &usize) -> bool {
        *lhs > *rhs
    }
}

impl Ord for usize {}

impl Add(usize) for usize {
    let Output :! Sized = usize;
    fn Add(lhs: usize, rhs: usize) -> usize {
        lhs + rhs
    }
}

impl Sub(usize) for usize {
    let Output :! Sized = usize;
    fn Sub(lhs: usize, rhs: usize) -> usize {
        lhs - rhs
    }
}

impl Mul(usize) for usize {
    let Output :! Sized = usize;
    fn Mul(lhs: usize, rhs: usize) -> usize {
        lhs * rhs
    }
}

impl Div(usize) for usize {
    let Output :! Sized = usize;
    fn Div(lhs: usize, rhs: usize) -> usize {
        lhs / rhs
    }
}

impl[T:! PartialEq(T)] PartialEq(&T) for &T {
    fn Eq(lhs: &&T, rhs: &&T) -> bool {
        (T as PartialEq(T)).Eq(*lhs, *rhs)
    }
}

impl[T:! PartialEq(T)] PartialEq(&mut T) for &mut T {
    fn Eq(lhs: &&mut T, rhs: &&mut T) -> bool {
        (T as PartialEq(T)).Eq(*lhs, *rhs)
    }
}

impl[T:! PartialOrd(T)] PartialOrd(&T) for &T {
    fn Lt(lhs: &&T, rhs: &&T) -> bool {
        (T as PartialOrd(T)).Lt(*lhs, *rhs)
    }
    fn Gt(lhs: &&T, rhs: &&T) -> bool {
        (T as PartialOrd(T)).Gt(*lhs, *rhs)
    }
}

impl[T:! PartialOrd(T)] PartialOrd(&mut T) for &mut T {
    fn Lt(lhs: &&mut T, rhs: &&mut T) -> bool {
        (T as PartialOrd(T)).Lt(*lhs, *rhs)
    }
    fn Gt(lhs: &&mut T, rhs: &&mut T) -> bool {
        (T as PartialOrd(T)).Gt(*lhs, *rhs)
    }
}

trait Into(T:! Sized): TryInto(T) {
    fn Into(self: Self) -> T;    
}

trait TryInto(T:! Sized): Sized {
    fn TryInto(self: Self) -> Option(T);
}
